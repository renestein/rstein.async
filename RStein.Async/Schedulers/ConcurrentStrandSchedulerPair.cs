using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RStein.Async.Threading;

namespace RStein.Async.Schedulers
{
  public sealed class ConcurrentStrandSchedulerPair : IDisposable
  {
    private readonly InterleaveExclusiveConcurrentTasksEngine m_interleaveExclusiveConcurrentTasksEngine;
    private bool m_isDisposed;

    public ConcurrentStrandSchedulerPair(int maxTasksConcurrency)
      : this(null, maxTasksConcurrency) {}

    public ConcurrentStrandSchedulerPair(TaskScheduler controlScheduler, int maxTasksConcurrency)
    {
      m_interleaveExclusiveConcurrentTasksEngine = new InterleaveExclusiveConcurrentTasksEngine(controlScheduler, maxTasksConcurrency);
      m_isDisposed = false;
    }

    public IProxyScheduler ConcurrentProxyScheduler
    {
      get
      {
        checkIfDisposed();
        return m_interleaveExclusiveConcurrentTasksEngine.ConcurrentProxyScheduler;
      }
    }

    public IProxyScheduler StrandProxyScheduler
    {
      get
      {
        checkIfDisposed();
        return m_interleaveExclusiveConcurrentTasksEngine.StrandProxyScheduler;
      }
    }

    public ITaskScheduler AsioStrandcheduler
    {
      get
      {
        checkIfDisposed();
        return m_interleaveExclusiveConcurrentTasksEngine.AsioStrandcheduler;
      }
    }

    public ITaskScheduler AsioConcurrentScheduler
    {
      get
      {
        checkIfDisposed();
        return m_interleaveExclusiveConcurrentTasksEngine.AsioConcurrentScheduler;
      }
    }


    public TaskScheduler ConcurrentScheduler
    {
      get
      {
        checkIfDisposed();
        return m_interleaveExclusiveConcurrentTasksEngine.ConcurrentProxyScheduler.AsTplScheduler();
      }
    }

    public TaskScheduler StrandScheduler
    {
      get
      {
        checkIfDisposed();
        return m_interleaveExclusiveConcurrentTasksEngine.StrandProxyScheduler.AsTplScheduler();
      }
    }

    public void Dispose()
    {
      if (m_isDisposed)
      {
        return;
      }

      Dispose(true);
      m_isDisposed = true;
    }

    private void Dispose(bool disposing)
    {
      if (disposing)
      {
        m_interleaveExclusiveConcurrentTasksEngine.Dispose();
      }
    }

    private void checkIfDisposed()
    {
      if (m_isDisposed)
      {
        throw new ObjectDisposedException(GetType().FullName);
      }
    }

    private class InterleaveExclusiveConcurrentTasksEngine
    {
      public const int MAX_STRAND_TASK_BATCH = 64;
      public const int CONCURRENT_TASK_BATCH_LIMIT = 64;
      public const int CONCURRENCY_TASK_BATCH_MULTIPLIER = 2;

      private const int CONTROL_SCHEDULER_CONCURRENCY = 1;
      private TaskCompletionSource<object> m_completedTcs;
      private AccumulateTasksSchedulerDecorator m_concurrentAccumulateScheduler;
      private IProxyScheduler m_concurrentProxyScheduler;
      private QueueTasksParams m_concurrentQueueTaskParams;

      private ThreadSafeSwitch m_concurrentTaskAdded;

      private TaskFactory m_controlTaskFactory;
      private QueueTasksParams m_exclusiveQueueTasksParams;
      private ThreadSafeSwitch m_exlusiveTaskAdded;
      private IoServiceThreadPoolScheduler m_ioControlScheduler;
      private bool m_isDisposed;
      private int m_maxConcurrentTaskBatch;
      private bool m_ownControlTaskScheduler;

      private Task m_processTaskLoop;
      private CancellationTokenSource m_stopCts;
      private AccumulateTasksSchedulerDecorator m_strandAccumulateScheduler;

      private IProxyScheduler m_strandProxyScheduler;
      private IoServiceThreadPoolScheduler m_threadPoolScheduler;

      public InterleaveExclusiveConcurrentTasksEngine(TaskScheduler controlScheduler, int maxTasksConcurrency)
      {
        init(controlScheduler, maxTasksConcurrency);
      }
      
      public IProxyScheduler ConcurrentProxyScheduler
      {
        get
        {
          return m_concurrentProxyScheduler;
        }
      }

      public IProxyScheduler StrandProxyScheduler
      {
        get
        {
          return m_strandProxyScheduler;
        }
      }

      public ITaskScheduler AsioConcurrentScheduler
      {
        get
        {
          return m_concurrentAccumulateScheduler;
        }
      }
      public ITaskScheduler AsioStrandcheduler
      {
        get
        {
          return m_strandAccumulateScheduler;
        }
      }

      private void init(TaskScheduler controlScheduler, int maxTasksConcurrency)
      {
        if (maxTasksConcurrency <= 0)
        {
          throw new ArgumentOutOfRangeException("maxTasksConcurrency");
        }

        m_maxConcurrentTaskBatch = Math.Min(CONCURRENT_TASK_BATCH_LIMIT, maxTasksConcurrency);

        m_exlusiveTaskAdded = new ThreadSafeSwitch();
        m_concurrentTaskAdded = new ThreadSafeSwitch();

        m_ownControlTaskScheduler = (controlScheduler == null);
        if (m_ownControlTaskScheduler)
        {
          var ioControlService = new IoServiceScheduler();
          m_ioControlScheduler = new IoServiceThreadPoolScheduler(ioControlService, CONTROL_SCHEDULER_CONCURRENCY);
          var controlProxyScheduler = new ProxyScheduler(m_ioControlScheduler);
          m_controlTaskFactory = new TaskFactory(controlProxyScheduler.AsTplScheduler());
        }
        else
        {
          m_controlTaskFactory = new TaskFactory(controlScheduler);
        }


        var ioService = new IoServiceScheduler();
        m_threadPoolScheduler = new IoServiceThreadPoolScheduler(ioService, maxTasksConcurrency);
        m_concurrentAccumulateScheduler = new AccumulateTasksSchedulerDecorator(m_threadPoolScheduler, _ => taskAdded(m_concurrentTaskAdded));
        var strandScheduler = new StrandSchedulerDecorator(m_threadPoolScheduler);
        var innerStrandProxyScheduler = new ProxyScheduler(strandScheduler);
        m_strandAccumulateScheduler = new AccumulateTasksSchedulerDecorator(strandScheduler, _ => taskAdded(m_exlusiveTaskAdded));
        m_strandProxyScheduler = new ProxyScheduler(m_strandAccumulateScheduler);
        m_concurrentProxyScheduler = new ProxyScheduler(m_concurrentAccumulateScheduler);
        m_processTaskLoop = null;
        m_completedTcs = new TaskCompletionSource<Object>();
        m_stopCts = new CancellationTokenSource();
        m_isDisposed = false;
      }

      private void taskAdded(ThreadSafeSwitch taskSwitch)
      {
        if (m_stopCts.IsCancellationRequested)
        {
          throw new InvalidOperationException("Could not add task - dispose is in progress");
        }

        taskSwitch.TrySet();
        isTaskLoopRequired();
      }

      private void isTaskLoopRequired()
      {
        if ((m_exlusiveTaskAdded.IsSet || m_concurrentTaskAdded.IsSet) && tryCreateLoopTask())
        {
          m_processTaskLoop.Start(m_controlTaskFactory.Scheduler);
        }

        trySetTaskLoopFinished();
      }

      private void trySetTaskLoopFinished()
      {
        if (m_stopCts.IsCancellationRequested)
        {
          m_completedTcs.TrySetResult(null);
        }
      }

      private bool tryCreateLoopTask()
      {
        var task = Interlocked.CompareExchange(ref m_processTaskLoop, new Task(runInnerTaskLoop), null);
        return (task == null);
      }

      private bool tryResetLoopTask()
      {
        var currentTask = m_processTaskLoop;
        var task = Interlocked.CompareExchange(ref m_processTaskLoop, null, currentTask);
        return (task != null);
      }

      private async void runInnerTaskLoop()
      {
        QueueTasksResult exclusiveQueueResult = null;
        QueueTasksResult concurrentQueueResult = null;

        do
        {
          do
          {
            m_exlusiveTaskAdded.TryReset();
            m_exclusiveQueueTasksParams = m_exclusiveQueueTasksParams ?? new QueueTasksParams(maxNumberOfQueuedtasks: MAX_STRAND_TASK_BATCH);
            exclusiveQueueResult = m_strandAccumulateScheduler.QueueTasksToInnerScheduler(m_exclusiveQueueTasksParams);
            await exclusiveQueueResult.WhenAllTask;
          } while (m_exlusiveTaskAdded.IsSet || exclusiveQueueResult.HasMoreTasks);

          do
          {
            m_concurrentTaskAdded.TryReset();
            m_concurrentQueueTaskParams = m_concurrentQueueTaskParams ?? new QueueTasksParams(maxNumberOfQueuedtasks: m_maxConcurrentTaskBatch);
            concurrentQueueResult = m_concurrentAccumulateScheduler.QueueTasksToInnerScheduler(m_concurrentQueueTaskParams);
            await concurrentQueueResult.WhenAllTask;
          } while (!m_exlusiveTaskAdded.IsSet && (m_concurrentTaskAdded.IsSet || concurrentQueueResult.HasMoreTasks));
        } while (existsTasksToProcess(exclusiveQueueResult, concurrentQueueResult));

        bool resetTaskResult = tryResetLoopTask();
        Debug.Assert(resetTaskResult);
        isTaskLoopRequired();
      }

      private bool existsTasksToProcess(QueueTasksResult exclusiveQueueResult, QueueTasksResult concurrentQueueResult)
      {
        return m_exlusiveTaskAdded.IsSet ||
               m_concurrentTaskAdded.IsSet ||
               exclusiveQueueResult.HasMoreTasks ||
               concurrentQueueResult.HasMoreTasks;
      }

      public void Dispose()
      {
        if (m_isDisposed)
        {
          return;
        }

        Dispose(true);
        m_isDisposed = true;
      }

      private void Dispose(bool disposing)
      {
        if (disposing)
        {
          m_stopCts.Cancel();
          isTaskLoopRequired();
          waitForCompletion();
          m_strandAccumulateScheduler.Dispose();
          m_concurrentAccumulateScheduler.Dispose();
          m_threadPoolScheduler.Dispose();

          if (m_ownControlTaskScheduler)
          {
            m_ioControlScheduler.Dispose();
          }
        }
      }

      private void waitForCompletion()
      {
        m_completedTcs.Task.Wait();
      }
    }
  }
}