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
    [Flags]
    private enum TryAddTaskResult
    {
      None = 0,
      Added = 1,
      ExecutedInline = 2,
      Rejected = 4
    }

    private const int DELAYED_TASKS_DEQUEUE_MS = 1;
    private const int MAX_CONCURRENCY_IN_STRAND = 1;
    private readonly ITaskScheduler m_originalScheduler;
    private readonly ThreadSafeSwitch m_canExecuteTaskSwitch;
    private readonly ConcurrentQueue<Task> m_tasks;
    private readonly ThreadLocal<bool> m_postOnCallStack;
    private readonly CancellationTokenSource m_delayedTaskDequeueCts;
    private readonly ConditionalWeakTable<Task, ThreadSafeSwitch> m_alreadyQueuedTasksTable;

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
        m_originalScheduler.ProxyScheduler = value;
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
      return Post(action, SchedulerRunCanceledToken);
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

    private bool taskAlreadyQueued(Task task)
    {
      ThreadSafeSwitch taskAlreadyQueued;
      return m_alreadyQueuedTasksTable.TryGetValue(task, out taskAlreadyQueued);

    }

    public override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {

      checkIfDisposed();

      if (!taskWasPreviouslyQueued || isCurrentThreadInPostMethodContext())
      {
        return false;
      }

      var tryAddTaskResult = tryProcessTask(task, taskWasPreviouslyQueued);

      if (tryAddTaskResult == TryAddTaskResult.Rejected)
      {
        return false;
      }

      if (tryAddTaskResult == TryAddTaskResult.Added)
      {
        m_alreadyQueuedTasksTable.GetOrCreateValue(task).TrySet();
        return false;
      }

      return true;

    }

    public virtual Action Wrap(Action action)
    {
      return () => Dispatch(action);
    }

    public virtual Func<Task> WrapAsTask(Action action)
    {
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
        Post(() =>
             {
               Trace.WriteLine("Running dispose task");
               SchedulerRunCancellationTokenSource.Cancel();
             }, CancellationToken.None).Wait();

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

      try
      {
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
        if (exceptionFromInnerTaskschedulerRaised && lockTaken)
        {
          resetTaskLock();
        }
      }
    }

    private Task Post(Action action, CancellationToken cancelToken)
    {
      checkIfDisposed();
      try
      {
        setPostMethodContext();
        return Task.Factory.StartNew(action, cancelToken, TaskCreationOptions.None, ProxyScheduler.AsRealScheduler());
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

      if (m_tasks.TryPeek(out nextTask))
      {
        TryExecuteTaskInline(nextTask, taskWasPreviouslyQueued: true);
      }
    }

    private bool isCurrentThreadInThisStrand()
    {
      Task currentTask;
      if (!m_tasks.TryDequeue(out currentTask))
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

  }
}