using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class StrandSchedulerDecorator : ITaskScheduler
  {
    [Flags]
    private enum TryAddTaskResult
    {
      None = 0,
      Added= 1,
      ExecutedInline = 2,
      AddedAndExecutedInline = 3,
      Rejected = 4
    }

    private const int MAX_CONCURRENCY = 1;
    private readonly ITaskScheduler m_originalScheduler;
    private SpinLock m_canExecuteTaskLock;

    private ConcurrentQueue<Task> m_tasks;

    public StrandSchedulerDecorator(ITaskScheduler originalScheduler)
    {
      if (originalScheduler == null)
      {
        throw new ArgumentNullException("originalScheduler");
      }

      m_originalScheduler = originalScheduler;
      
      m_tasks = new ConcurrentQueue<Task>();
      m_canExecuteTaskLock = new SpinLock();
      
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
    }

    public virtual bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      if (TryAddTaskResult(task, taskWasPreviouslyQueued))
      {

        return true;
      }
      
      m_tasks.Enqueue(task);
      return false;

    }

    public IEnumerable<Task> GetScheduledTasks()
    {
      return m_tasks.ToArray();
    }

    public void Dispose()
    {
      throw new NotImplementedException();
    }

    private TryAddTaskResult TryAddTask(Task task, bool taskWasPreviouslyQueued)
    {
      bool lockTaken = false;
      bool exceptionFromInnerTaskschedulerRaised = false;
      m_canExecuteTaskLock.TryEnter(ref lockTaken);
      
      if (!lockTaken)
      {
        return TryAddTaskResult.Rejected;
      }

      try
      {
        addTaskContinuationHandler(task);
        bool taskExecutedInline = m_originalScheduler.TryExecuteTaskInline(task, taskWasPreviouslyQueued);
        return taskExecutedInline ? TryAddTaskResult.AddedAndExecutedInline : TryAddTaskResult.Added;

      }
      catch(Exception ex)
      {
        Trace.WriteLine(ex);
        exceptionFromInnerTaskschedulerRaised = true;
      }
      finally
      {
        if (lockTaken && exceptionFromInnerTaskschedulerRaised)
        {
          m_canExecuteTaskLock.Exit();
        }
      }
      
      return TryAddTaskResult.Rejected;
    }

    private void addTaskContinuationHandler(Task task)
    {
      task.ContinueWith(_ =>
                        {
                          m_canExecuteTaskLock.Exit();
                          tryPopNextTask();
                        });
    }

    private void tryPopNextTask()
    {
      
    }
  }
}