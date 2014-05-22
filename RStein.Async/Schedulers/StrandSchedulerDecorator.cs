using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RStein.Async.Tasks;
using RStein.Async.Threading;

namespace RStein.Async.Schedulers
{
  public class StrandSchedulerDecorator : TaskSchedulerBase, IAsioTaskService
  {
    private const int DELAYED_TASKS_DEQUEUE_MS = 0;
    private const int MAX_CONCURRENCY_IN_STRAND = 1;
    private readonly ConditionalWeakTable<Task, ThreadSafeSwitch> m_alreadyQueuedTasksTable;
    private readonly ThreadSafeSwitch m_canExecuteTaskSwitch;
    private readonly CancellationTokenSource m_delayedTaskDequeueCts;
    private readonly ITaskScheduler m_originalScheduler;
    private readonly ThreadLocal<bool> m_postOnCallStack;
    private readonly ConcurrentQueue<Task> m_tasks;

    public StrandSchedulerDecorator(ITaskScheduler originalScheduler)
    {
      if (originalScheduler == null)
      {
        throw new ArgumentNullException("originalScheduler");
      }

      m_originalScheduler = originalScheduler;

      m_tasks = new ConcurrentQueue<Task>();
      m_canExecuteTaskSwitch = new ThreadSafeSwitch();
      m_postOnCallStack = new ThreadLocal<bool>(() => false);
      m_delayedTaskDequeueCts = new CancellationTokenSource();
      m_alreadyQueuedTasksTable = new ConditionalWeakTable<Task, ThreadSafeSwitch>();
    }

    public override int MaximumConcurrencyLevel
    {
      get
      {
        checkIfDisposed();
        return MAX_CONCURRENCY_IN_STRAND;
      }
    }

    public override IExternalProxyScheduler ProxyScheduler
    {
      get
      {
        checkIfDisposed();
        return base.ProxyScheduler;
      }
      set
      {
        checkIfDisposed();
        if (m_originalScheduler.ProxyScheduler == null)
        {
          m_originalScheduler.ProxyScheduler = value;
        }
        base.ProxyScheduler = value;
      }
    }

    public virtual Task Dispatch(Action action)
    {
      checkIfDisposed();

      if (isCurrentThreadInThisStrand())
      {
        return TaskEx.TaskFromSynchronnousAction(action);
      }

      return Post(action);
    }

    public virtual Task Post(Action action)
    {
      Func<Task> postTaskFunc = () => Task.Factory.StartNew(action,
        SchedulerRunCanceledToken,
        TaskCreationOptions.None,
        ProxyScheduler.AsRealScheduler());
      return postToScheduler(postTaskFunc);
    }

    public virtual Action Wrap(Action action)
    {
      checkIfDisposed();
      return () => Dispatch(action);
    }

    public virtual Func<Task> WrapAsTask(Action action)
    {
      checkIfDisposed();
      return () => Dispatch(action);
    }

    public virtual Task Dispatch(Func<Task> function)
    {
      if (isCurrentThreadInThisStrand())
      {
        return function();
      }

      return Post(function);
    }

    public Task Post(Func<Task> function)
    {
      Func<Task> postTaskFunc = () => Task.Factory.StartNew(function,
        SchedulerRunCanceledToken,
        TaskCreationOptions.None,
        ProxyScheduler.AsRealScheduler()).Unwrap();

      return postToScheduler(postTaskFunc);
    }

    public virtual bool RunningInThisThread()
    {
      return isCurrentThreadInThisStrand();
    }


    private void resetPostMethodContext()
    {
      m_postOnCallStack.Value = false;
    }


    public override void QueueTask(Task task)
    {
      checkIfDisposed();

      if (taskAlreadyQueued(task))
      {
        return;
      }

      m_tasks.Enqueue(task);

      if (!isCurrentThreadInPostMethodContext())
      {
        tryExecuteNextTask();
      }
      else
      {
        Task.Delay(DELAYED_TASKS_DEQUEUE_MS, m_delayedTaskDequeueCts.Token)
          .ContinueWith(_ => tryExecuteNextTask(), TaskScheduler.Default);
      }
    }

    public override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      return safeTryExecuteTaskInline(task, taskWasPreviouslyQueued, callFromStrand: false);
    }

    private bool safeTryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued, bool callFromStrand = false)
    {
      checkIfDisposed();

      if (!callFromStrand || !taskWasPreviouslyQueued || isCurrentThreadInPostMethodContext())
      {
        return false;
      }

      var tryAddTaskResult = tryProcessTask(task, taskWasPreviouslyQueued);

      if (tryAddTaskResult == TryAddTaskResult.Rejected)
      {
        return false;
      }

      return (tryAddTaskResult == TryAddTaskResult.ExecutedInline);
    }

    private bool taskAlreadyQueued(Task task)
    {
      ThreadSafeSwitch taskAlreadyQueued;
      return m_alreadyQueuedTasksTable.TryGetValue(task, out taskAlreadyQueued);
    }

    public virtual Action Wrap(Func<Task> function)
    {
      checkIfDisposed();
      return () => Dispatch(function);
    }


    public virtual Func<Task> WrapAsTask(Func<Task> action)
    {
      checkIfDisposed();
      return () => Dispatch(action);
    }

    public override IEnumerable<Task> GetScheduledTasks()
    {
      checkIfDisposed();
      return m_tasks.ToArray();
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        Func<Task> disposeAction = () => Task.Factory.StartNew(() =>
                                                               {
                                                                 Trace.WriteLine("Running dispose task");
                                                                 SchedulerRunCancellationTokenSource.Cancel();
                                                               }, CancellationToken.None, TaskCreationOptions.None, ProxyScheduler.AsRealScheduler());

        var disposeTask = postToScheduler(disposeAction);

        disposeTask.Wait();

        m_postOnCallStack.Dispose();
      }
    }

    private TryAddTaskResult tryProcessTask(Task task, bool taskWasPreviouslyQueued)
    {
      var exceptionFromInnerTaskschedulerRaised = false;
      var lockTaken = false;

      if (!isOriginalSchedulerInImplicitStrand())
      {
        lockTaken = m_canExecuteTaskSwitch.TrySet();

        if (!lockTaken)
        {
          return TryAddTaskResult.Rejected;
        }
      }


      bool canExecuteTask = false;


      try
      {
        canExecuteTask = m_alreadyQueuedTasksTable.GetOrCreateValue(task).TrySet();
        if (!canExecuteTask)
        {
          return TryAddTaskResult.AlreadyAdded;
        }

        addTaskContinuation(task, taskWasPreviouslyQueued, lockTaken);
        return executeTaskOnInnerScheduler(task);
      }
      catch (Exception ex)
      {
        Trace.WriteLine(ex);
        exceptionFromInnerTaskschedulerRaised = true;
        throw;
      }
      finally
      {
        if ((exceptionFromInnerTaskschedulerRaised || !canExecuteTask) && lockTaken)
        {
          resetTaskLock();
        }
      }
    }

    private Task postToScheduler(Func<Task> postFunction)
    {
      checkIfDisposed();
      try
      {
        setPostMethodContext();
        return postFunction();
      }
      finally
      {
        resetPostMethodContext();
      }
    }

    private TryAddTaskResult executeTaskOnInnerScheduler(Task task)
    {
      bool taskExecutedInline = m_originalScheduler.TryExecuteTaskInline(task, false);

      if (taskExecutedInline)
      {
        return TryAddTaskResult.ExecutedInline;
      }

      m_originalScheduler.QueueTask(task);
      return TryAddTaskResult.Added;
    }

    private bool isOriginalSchedulerInImplicitStrand()
    {
      return (m_originalScheduler.MaximumConcurrencyLevel == MAX_CONCURRENCY_IN_STRAND);
    }

    private void addTaskContinuation(Task task, bool taskWasPreviouslyQueued, bool withLock)
    {
      task.ContinueWith(previousTask =>
                        {
                          try
                          {
                            if (taskWasPreviouslyQueued)
                            {
                              popQueuedTask(previousTask);
                            }
                          }
                          finally
                          {
                            if (withLock)
                            {
                              resetTaskLock();
                            }
                          }

                          tryExecuteNextTask();
                        }, TaskScheduler.Default);
    }

    private void popQueuedTask(Task previousTask)
    {
      Task task;
      bool result = m_tasks.TryDequeue(out task);
      Debug.Assert(result);
      Debug.Assert(ReferenceEquals(task, previousTask));
    }

    private void resetTaskLock()
    {
      bool lockReset = m_canExecuteTaskSwitch.TryReset();
      Debug.Assert(lockReset);
    }

    private void tryExecuteNextTask()
    {
      Task nextTask;

      if (m_tasks.TryPeek(out nextTask) && !taskAlreadyQueued(nextTask))
      {
        safeTryExecuteTaskInline(nextTask, taskWasPreviouslyQueued: true, callFromStrand: true);
      }
    }

    private bool isCurrentThreadInThisStrand()
    {
      Task currentTask;
      if (!m_tasks.TryPeek(out currentTask))
      {
        return false;
      }

      return currentTask.Id == Task.CurrentId;
    }

    private void setPostMethodContext()
    {
      m_postOnCallStack.Value = true;
    }

    private bool isCurrentThreadInPostMethodContext()
    {
      return m_postOnCallStack.Value;
    }

    [Flags]
    private enum TryAddTaskResult
    {
      None = 0,
      Added = 1,
      ExecutedInline = 2,
      Rejected = 4,
      AlreadyAdded = 8
    }
  }
}