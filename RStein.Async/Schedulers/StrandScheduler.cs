using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using RStein.Async.Threading;

namespace RStein.Async.Schedulers
{
  public class StrandSchedulerDecorator : ITaskScheduler
  {
    [Flags]
    private enum TryAddTaskResult
    {
      None = 0,
      Added = 1,
      ExecutedInline = 2,
      Rejected = 4
    }

    private const int MAX_CONCURRENCY = 1;
    private readonly ITaskScheduler m_originalScheduler;
    private ThreadSafeSwitch m_canExecuteTaskSwitch;

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

    public virtual int MaximumConcurrencyLevel
    {
      get
      {
        return MAX_CONCURRENCY;
      }
    }
    public void SetProxyScheduler(IExternalProxyScheduler scheduler)
    {
      m_originalScheduler.SetProxyScheduler(scheduler);

    }

    public virtual void QueueTask(Task task)
    {
      m_tasks.Enqueue(task);
      tryExecuteNextTask();
    }

    public virtual bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
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

    public virtual IEnumerable<Task> GetScheduledTasks()
    {
      return m_tasks.ToArray();
    }

    public void Dispose()
    {
      throw new NotImplementedException();
    }

    private TryAddTaskResult TryAddTask(Task task, bool taskWasPreviouslyQueued)
    {
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
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (lockTaken && exceptionFromInnerTaskschedulerRaised)
        {
          resetTaskLock();
        }
      }

      return TryAddTaskResult.Rejected;
    }

    private void addTaskContinuationHandler(Task task, bool taskWasPreviouslyQueued)
    {
      task.ContinueWith(previousTask =>
                        {
                          if (taskWasPreviouslyQueued)
                          {
                            popQueuedTask(previousTask);
                          }
                          resetTaskLock();
                          tryExecuteNextTask();
                        });
    }

    private void popQueuedTask(Task previousTask)
    {
      Task task = null;
      bool result = m_tasks.TryDequeue(out task);
      Debug.Assert(result);
      Debug.Assert(Object.ReferenceEquals(task, previousTask));
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