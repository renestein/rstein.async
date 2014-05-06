using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using RStein.Async.Tasks;
using RStein.Async.Threading;

namespace RStein.Async.Schedulers
{
  public class AccumulateTasksSchedulerDecorator : TaskSchedulerBase
  {
    private readonly ITaskScheduler m_innerScheduler;
    private readonly Action<Task> m_newTaskQueuedAction;
    private ConcurrentQueue<Task> m_tasks;
    private ThreadSafeSwitch m_queingToInnerSchedulerSwitch;

    public AccumulateTasksSchedulerDecorator(ITaskScheduler innerScheduler, Action<Task> newTaskQueuedAction)
    {
      if (innerScheduler == null)
      {
        throw new ArgumentNullException("innerScheduler");
      }

      m_innerScheduler = innerScheduler;
      m_newTaskQueuedAction = newTaskQueuedAction;
      m_tasks = new ConcurrentQueue<Task>();
      m_queingToInnerSchedulerSwitch = new ThreadSafeSwitch();
    }

    public override int MaximumConcurrencyLevel
    {
      get
      {
        return m_innerScheduler.MaximumConcurrencyLevel;
      }
    }

    public override IExternalProxyScheduler ProxyScheduler
    {
      get
      {
        return base.ProxyScheduler;
      }
      set
      {
        if (m_innerScheduler.ProxyScheduler != null)
        {
          m_innerScheduler.ProxyScheduler = value;
        }

        base.ProxyScheduler = value;
      }
    }

    public override void QueueTask(Task task)
    {
      m_tasks.Enqueue(task);
      m_newTaskQueuedAction(task);
    }

    public override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      return false;
    }

    public override IEnumerable<Task> GetScheduledTasks()
    {
      return m_tasks.ToArray();
    }

    public virtual Tuple<int, Task> QueueAllTasksToInnerScheduler(Action<Task> beforeTaskQueuedAction = null, Action<Task> taskContinuation = null, Action<Task> afterTaskQueuedAction = null)
    {
      var currentTasks = new List<Task>();
      try
      {
        if (!m_queingToInnerSchedulerSwitch.TrySet())
        {
          return Tuple.Create(0, PredefinedTasks.CompletedTask);
        }

        Task task;

        while (m_tasks.TryDequeue(out task))
        {
          currentTasks.Add(task);
          if (beforeTaskQueuedAction != null)
          {
            beforeTaskQueuedAction(task);
          }

          if (taskContinuation != null)
          {
            task.ContinueWith(taskContinuation);
          }

          m_innerScheduler.QueueTask(task);

          if (afterTaskQueuedAction != null)
          {
            afterTaskQueuedAction(task);
          }

        }
      }
      finally
      {
        m_queingToInnerSchedulerSwitch.TryReset();
      }

      var whenAllTask = Task.WhenAll(currentTasks);
      var retPair = Tuple.Create(currentTasks.Count, whenAllTask);
      return retPair;
    }

    protected override void Dispose(bool disposing)
    {
      Debug.Assert(m_tasks.Count == 0);
      m_innerScheduler.Dispose();
    }
  }
}