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
      m_originalScheduler = originalScheduler ?? throw new ArgumentNullException(nameof(originalScheduler));

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
        CheckIfDisposed();
        return MAX_CONCURRENCY_IN_STRAND;
      }
    }

    public override IProxyScheduler ProxyScheduler
    {
      get
      {
        CheckIfDisposed();
        return base.ProxyScheduler;
      }
      set
      {
        CheckIfDisposed();
        if (m_originalScheduler.ProxyScheduler == null)
        {
          m_originalScheduler.ProxyScheduler = value;
        }
        base.ProxyScheduler = value;
      }
    }

    public virtual Task Dispatch(Action action)
    {
      CheckIfDisposed();

      if (isCurrentThreadInThisStrand())
      {
        return TaskEx.TaskFromSynchronnousAction(action);
      }

      return Post(action);
    }

    public virtual Task Dispatch(Func<Task> function)
    {
      if (isCurrentThreadInThisStrand())
      {
        return function();
      }

      return Post(function);
    }

    public virtual Task Post(Action action)
    {
      Task PostTaskFunc() => Task.Factory.StartNew(action,
        SchedulerRunCanceledToken,
        TaskCreationOptions.None,
        ProxyScheduler.AsTplScheduler());

      return postToScheduler(PostTaskFunc);
    }

    public Task Post(Func<Task> function)
    {
      Task PostTaskFunc() => Task.Factory.StartNew(function,
                            SchedulerRunCanceledToken,
                            TaskCreationOptions.None,
                            ProxyScheduler.AsTplScheduler())
        .Unwrap();

      return postToScheduler(PostTaskFunc);
    }

    public virtual Action Wrap(Action action)
    {
      CheckIfDisposed();
      return () => Dispatch(action);
    }

    public virtual Action Wrap(Func<Task> function)
    {
      CheckIfDisposed();
      return () => Dispatch(function);
    }

    public virtual Func<Task> WrapAsTask(Action action)
    {
      CheckIfDisposed();
      return () => Dispatch(action);
    }

    public virtual Func<Task> WrapAsTask(Func<Task> action)
    {
      CheckIfDisposed();
      return () => Dispatch(action);
    }

    public virtual bool RunningInThisThread()
    {
      CheckIfDisposed();
      return isCurrentThreadInThisStrand();
    }


    public override void QueueTask(Task task)
    {
      CheckIfDisposed();

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
      CheckIfDisposed();
      return safeTryExecuteTaskInline(task, taskWasPreviouslyQueued, callFromStrand: false);
    }


    public override IEnumerable<Task> GetScheduledTasks()
    {
      CheckIfDisposed();
      return m_tasks.ToArray();
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        Task DisposeAction() => Task.Factory.StartNew(() =>
        {
          Trace.WriteLine("Running dispose task");
          SchedulerRunCancellationTokenSource.Cancel();
        }, CancellationToken.None,
          TaskCreationOptions.None,
          ProxyScheduler.AsTplScheduler());

        var disposeTask = postToScheduler(DisposeAction);

        disposeTask.Wait();

        m_postOnCallStack.Dispose();
      }
    }

    private void resetPostMethodContext()
    {
      m_postOnCallStack.Value = false;
    }


    private bool safeTryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued, bool callFromStrand = false)
    {
      CheckIfDisposed();

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
      return m_alreadyQueuedTasksTable.TryGetValue(task, out var taskAlreadyQueued);
    }

    private TryAddTaskResult tryProcessTask(Task task, bool taskWasPreviouslyQueued)
    {
      var exceptionFromInnerTaskSchedulerRaised = false;
      var lockTaken = false;

      if (!isOriginalSchedulerInImplicitStrand())
      {
        lockTaken = m_canExecuteTaskSwitch.TrySet();

        if (!lockTaken)
        {
          return TryAddTaskResult.Rejected;
        }
      }


      var canExecuteTask = false;


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
        exceptionFromInnerTaskSchedulerRaised = true;
        throw;
      }
      finally
      {
        if ((exceptionFromInnerTaskSchedulerRaised || !canExecuteTask) && lockTaken)
        {
          resetTaskLock();
        }
      }
    }

    private Task postToScheduler(Func<Task> postFunction)
    {
      CheckIfDisposed();
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
      var taskExecutedInline = m_originalScheduler.TryExecuteTaskInline(task, false);

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
      var result = m_tasks.TryDequeue(out var task);
      Debug.Assert(result);
      Debug.Assert(ReferenceEquals(task, previousTask));
    }

    private void resetTaskLock()
    {
      var lockReset = m_canExecuteTaskSwitch.TryReset();
      Debug.Assert(lockReset);
    }

    private void tryExecuteNextTask()
    {
      if (m_tasks.TryPeek(out var nextTask) && !taskAlreadyQueued(nextTask))
      {
        safeTryExecuteTaskInline(nextTask, taskWasPreviouslyQueued: true, callFromStrand: true);
      }
    }

    private bool isCurrentThreadInThisStrand()
    {
      if (!m_tasks.TryPeek(out var currentTask))
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