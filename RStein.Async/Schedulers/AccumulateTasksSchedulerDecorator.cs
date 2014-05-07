using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using RStein.Async.Tasks;
using RStein.Async.Threading;

namespace RStein.Async.Schedulers
{
  public class AccumulateTasksSchedulerDecorator : TaskSchedulerBase
  {
    private readonly ITaskScheduler m_innerScheduler;
    private readonly Action<Task> m_newTaskQueuedAction;
    private readonly ConcurrentQueue<Task> m_tasks;
    private readonly ThreadSafeSwitch m_queingToInnerSchedulerSwitch;

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
        checkIfDisposed();
        return m_innerScheduler.MaximumConcurrencyLevel;
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
        if (m_innerScheduler.ProxyScheduler == null)
        {
          m_innerScheduler.ProxyScheduler = value;
        }

        base.ProxyScheduler = value;
      }
    }

    public override void QueueTask(Task task)
    {
      checkIfDisposed();
      m_tasks.Enqueue(task);

      if (m_newTaskQueuedAction != null)
      {
        m_newTaskQueuedAction(task);
      }
    }

    public override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      checkIfDisposed();
      return false;
    }

    public override IEnumerable<Task> GetScheduledTasks()
    {
      checkIfDisposed();
      return m_tasks.ToArray();
    }

    public virtual QueueTasksResult QueueAllTasksToInnerScheduler(QueueTasksParams queueTasksParams = null)
    {
      checkIfDisposed();

      var currentParams = queueTasksParams ?? new QueueTasksParams();
      var currentTasks = new List<Task>();

      bool hasMoreTasks;

      try
      {
        if (!m_queingToInnerSchedulerSwitch.TrySet())
        {
          return new QueueTasksResult(numberOfQueuedTasks: 0,
                                       whenAllTask: PredefinedTasks.CompletedTask,
                                       hasMoreTasks: false);
        }

        Task task;

        while (canQueueTask(currentParams, currentTasks) && m_tasks.TryDequeue(out task))
        {
          currentTasks.Add(task);
          if (currentParams.BeforeTaskQueuedAction != null)
          {
            currentParams.BeforeTaskQueuedAction(task);
          }

          if (currentParams.TaskContinuation != null)
          {
            task.ContinueWith(currentParams.TaskContinuation);
          }

          m_innerScheduler.QueueTask(task);

          if (currentParams.AfterTaskQueuedAction != null)
          {
            currentParams.AfterTaskQueuedAction(task);
          }

        }

        hasMoreTasks = m_tasks.TryPeek(out task);
      }
      finally
      {
        m_queingToInnerSchedulerSwitch.TryReset();
      }

      var whenAllTask = Task.WhenAll(currentTasks);
      var result = new QueueTasksResult(numberOfQueuedTasks: currentTasks.Count, 
                                        whenAllTask: whenAllTask, 
                                        hasMoreTasks: hasMoreTasks);
      return result;
    }

    private static bool canQueueTask(QueueTasksParams currentParams, List<Task> currentTasks)
    {
      return currentParams.MaxNumberOfQueuedtasks < currentTasks.Count;
    }

    protected override void Dispose(bool disposing)
    {
      QueueAllTasksToInnerScheduler(new QueueTasksParams()).WhenAllTask.Wait();
    }
  }
}