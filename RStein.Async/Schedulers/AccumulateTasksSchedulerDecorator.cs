using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RStein.Async.Tasks;
using RStein.Async.Threading;

namespace RStein.Async.Schedulers
{
  public class AccumulateTasksSchedulerDecorator : TaskSchedulerBase
  {
    private readonly ThreadLocal<bool> m_ignoreCancellationToken;
    private readonly ITaskScheduler m_innerScheduler;
    private readonly Action<Task> m_newTaskQueuedAction;
    private readonly ThreadSafeSwitch m_queingToInnerSchedulerSwitch;
    private readonly ConcurrentQueue<Task> m_tasks;

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
      m_ignoreCancellationToken = new ThreadLocal<bool>(() => false);
    }

    public override int MaximumConcurrencyLevel
    {
      get
      {
        checkIfDisposed();
        return m_innerScheduler.MaximumConcurrencyLevel;
      }
    }

    public override IProxyScheduler ProxyScheduler
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

    public virtual QueueTasksResult QueueTasksToInnerScheduler(QueueTasksParams queueTasksParams = null)
    {
      checkIfDisposed();

      var currentParams = queueTasksParams ?? new QueueTasksParams();
      var currentTasks = new List<Task>();

      bool hasMoreTasks = false;

      try
      {
        if (processingCanceled())
        {
          return new QueueTasksResult(numberOfQueuedTasks: 0,
            whenAllTask: PredefinedTasks.CompletedTask,
            hasMoreTasks: false);
        }

        if (!m_queingToInnerSchedulerSwitch.TrySet())
        {
          return new QueueTasksResult(numberOfQueuedTasks: 0,
            whenAllTask: PredefinedTasks.CompletedTask,
            hasMoreTasks: !m_tasks.IsEmpty);
        }

        queueTasks(currentParams, currentTasks);

        hasMoreTasks = !m_tasks.IsEmpty;
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

    private bool processingCanceled()
    {
      return SchedulerRunCanceledToken.IsCancellationRequested && !m_ignoreCancellationToken.Value;
    }

    private void queueTasks(QueueTasksParams currentParams, List<Task> currentTasks)
    {
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
    }

    private static bool canQueueTask(QueueTasksParams currentParams, List<Task> currentTasks)
    {
      return currentTasks.Count < currentParams.MaxNumberOfQueuedtasks;
    }

    protected override void Dispose(bool disposing)
    {
      try
      {
        m_ignoreCancellationToken.Value = true;
        SchedulerRunCancellationTokenSource.Cancel();
        QueueTasksResult result;
        do
        {
          result = QueueTasksToInnerScheduler();
          result.WhenAllTask.Wait();
        } while (result.HasMoreTasks);
      }
      finally
      {
        m_ignoreCancellationToken.Value = false;
      }

      m_ignoreCancellationToken.Dispose();
    }
  }
}