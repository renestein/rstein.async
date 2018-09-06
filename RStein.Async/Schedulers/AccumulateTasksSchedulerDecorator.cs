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
    private readonly ThreadSafeSwitch m_queueToInnerSchedulerSwitch;
    private readonly ConcurrentQueue<Task> m_tasks;

    public AccumulateTasksSchedulerDecorator(ITaskScheduler innerScheduler, Action<Task> newTaskQueuedAction)
    {
      m_innerScheduler = innerScheduler ?? throw new ArgumentNullException(nameof(innerScheduler));
      m_newTaskQueuedAction = newTaskQueuedAction;
      m_tasks = new ConcurrentQueue<Task>();
      m_queueToInnerSchedulerSwitch = new ThreadSafeSwitch();
      m_ignoreCancellationToken = new ThreadLocal<bool>(() => false);
    }

    public override int MaximumConcurrencyLevel
    {
      get
      {
        CheckIfDisposed();
        return m_innerScheduler.MaximumConcurrencyLevel;
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
        if (m_innerScheduler.ProxyScheduler == null)
        {
          m_innerScheduler.ProxyScheduler = value;
        }

        base.ProxyScheduler = value;
      }
    }

    public override void QueueTask(Task task)
    {
      CheckIfDisposed();
      m_tasks.Enqueue(task);

      m_newTaskQueuedAction?.Invoke(task);
    }

    public override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      CheckIfDisposed();
      return false;

    }

    public override IEnumerable<Task> GetScheduledTasks()
    {
      CheckIfDisposed();
      return m_tasks.ToArray();
    }

    public virtual QueueTasksResult QueueTasksToInnerScheduler(QueueTasksParams queueTasksParams = null)
    {
      CheckIfDisposed();

      var currentParams = queueTasksParams ?? new QueueTasksParams();
      var currentTasks = new List<Task>();

      var hasMoreTasks = false;

      try
      {
        if (processingCanceled())
        {
          return new QueueTasksResult(numberOfQueuedTasks: 0,
            whenAllTask: PredefinedTasks.CompletedTask,
            hasMoreTasks: false);
        }

        if (!m_queueToInnerSchedulerSwitch.TrySet())
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
        m_queueToInnerSchedulerSwitch.TryReset();
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
      while (canQueueTask(currentParams, currentTasks) && m_tasks.TryDequeue(out var task))
      {
        currentTasks.Add(task);
        currentParams.BeforeTaskQueuedAction?.Invoke(task);

        if (currentParams.TaskContinuation != null)
        {
          task.ContinueWith(currentParams.TaskContinuation);
        }

        m_innerScheduler.QueueTask(task);

        currentParams.AfterTaskQueuedAction?.Invoke(task);
      }
    }

    private static bool canQueueTask(QueueTasksParams currentParams, List<Task> currentTasks)
    {
      return currentTasks.Count < currentParams.MaxNumberOfQueuedTasks;
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