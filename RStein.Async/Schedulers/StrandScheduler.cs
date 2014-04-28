using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RStein.Async.Threading;

namespace RStein.Async.Schedulers
{
  public class StrandSchedulerDecorator : TaskSchedulerBase
  {
    [Flags]
    private enum TryAddTaskResult
    {
      None = 0,
      Added = 1,
      ExecutedInline = 2,
      Rejected = 4
    }

    private const int MAX_CONCURRENCY_IN_STRAND = 1;
    private readonly ITaskScheduler m_originalScheduler;
    private readonly ThreadSafeSwitch m_canExecuteTaskSwitch;
    private ConcurrentQueue<Task> m_tasks;

    public StrandSchedulerDecorator(ITaskScheduler originalScheduler)
    {
      if (originalScheduler == null)
      {
        throw new ArgumentNullException("originalScheduler");
      }

      m_originalScheduler = originalScheduler;

      m_tasks = new ConcurrentQueue<Task>();
      m_canExecuteTaskSwitch = new ThreadSafeSwitch();

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
      var myTask = new Task(action);
      Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, ProxyScheduler.AsRealScheduler());
      return myTask;
    }

    public virtual Task Post(Action action)
    {
      checkIfDisposed();
      var task = Dispatch(action);
      return task;
    }

    public override void QueueTask(Task task)
    {
      checkIfDisposed();
      m_tasks.Enqueue(task);
      tryExecuteNextTask();
    }

    public override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {

      checkIfDisposed();
      var tryAddTaskResult = TryAddTask(task, taskWasPreviouslyQueued);

      if (tryAddTaskResult == TryAddTaskResult.Rejected)
      {
        if (!taskWasPreviouslyQueued)
        {
          m_tasks.Enqueue(task);
        }

        return false;
      }

      if (tryAddTaskResult == TryAddTaskResult.Added)
      {
        return false;
      }

      return true;

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
        Post(() => Trace.WriteLine("Running dispose task")).Wait();
      }
    }

    private TryAddTaskResult TryAddTask(Task task, bool taskWasPreviouslyQueued)
    {

      if (isOriginalSchedulerInImplicitStrand())
      {

      }
      bool exceptionFromInnerTaskschedulerRaised = false;
      bool lockTaken = m_canExecuteTaskSwitch.TrySet();

      if (!lockTaken)
      {
        return TryAddTaskResult.Rejected;
      }

      try
      {
        addTaskContinuationHandler(task, taskWasPreviouslyQueued);
        bool taskExecutedInline = m_originalScheduler.TryExecuteTaskInline(task, taskWasPreviouslyQueued);
        return taskExecutedInline ? TryAddTaskResult.ExecutedInline : TryAddTaskResult.Added;

      }
      catch (Exception ex)
      {
        Trace.WriteLine(ex);
        exceptionFromInnerTaskschedulerRaised = true;
        throw;
      }
      finally
      {
        if (exceptionFromInnerTaskschedulerRaised)
        {
          resetTaskLock();
        }
      }
    }

    private bool isOriginalSchedulerInImplicitStrand()
    {
      return (m_originalScheduler.MaximumConcurrencyLevel == MAX_CONCURRENCY_IN_STRAND);
    }

    private void addTaskContinuationHandler(Task task, bool taskWasPreviouslyQueued)
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
                            resetTaskLock();
                          }
                          tryExecuteNextTask();
                        });
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
  }
}