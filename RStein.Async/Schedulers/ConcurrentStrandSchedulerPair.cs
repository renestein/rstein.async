using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RStein.Async.Threading;

namespace RStein.Async.Schedulers
{
  public sealed class ConcurrentStrandSchedulerPair : IDisposable
  {
    private readonly InterleaveTaskSource m_interleaveTaskSource;
    private bool m_isDisposed;

    public ConcurrentStrandSchedulerPair(int maxTasksConcurrency)
      : this(null, maxTasksConcurrency)
    {

    }

    public ConcurrentStrandSchedulerPair(TaskScheduler controlScheduler, int maxTasksConcurrency)
    {
      m_interleaveTaskSource = new InterleaveTaskSource(controlScheduler, maxTasksConcurrency);
      m_isDisposed = false;
    }

    public IExternalProxyScheduler ConcurrentProxyScheduler
    {
      get
      {
        checkIfDisposed();
        return m_interleaveTaskSource.ConcurrentProxyScheduler;
      }
    }

    public IExternalProxyScheduler StrandProxyScheduler
    {
      get
      {
        checkIfDisposed();
        return m_interleaveTaskSource.StrandProxyScheduler;
      }
    }

    public ITaskScheduler AsioStrandcheduler
    {
      get
      {
        checkIfDisposed();
        return m_interleaveTaskSource.AsioStrandcheduler;
      }
    }

    public ITaskScheduler AsioConcurrentScheduler
    {
      get
      {
        checkIfDisposed();
        return m_interleaveTaskSource.AsioConcurrentScheduler;
      }
    }


    public TaskScheduler ConcurrentScheduler
    {
      get
      {
        checkIfDisposed();
        return m_interleaveTaskSource.ConcurrentProxyScheduler.AsRealScheduler();
      }
    }

    public TaskScheduler StrandScheduler
    {
      get
      {
        checkIfDisposed();
        return m_interleaveTaskSource.StrandProxyScheduler.AsRealScheduler();
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
        m_interleaveTaskSource.Dispose();
      }
    }

    private void checkIfDisposed()
    {
      if (m_isDisposed)
      {
        throw new ObjectDisposedException(GetType().FullName);
      }
    }

    private class InterleaveTaskSource
    {
      public const int MAX_STRAND_TASK_BATCH = 64;
      public const int CONCURRENT_TASK_BATCH_LIMIT = 64;
      public const int CONCURRENCY_TASK_BATCH_MULTIPLIER = 2;

      private const int CONTROL_SCHEDULER_CONCURRENCY = 1;

      private int m_maxConcurrentTaskBatch;
      private ThreadSafeSwitch m_exlusiveTaskAdded;
      private ThreadSafeSwitch m_concurrentTaskAdded;

      private TaskFactory m_controlTaskFactory;
      private bool m_ownControlTaskScheduler;
      private AccumulateTasksSchedulerDecorator m_concurrentAccumulateScheduler;
      private AccumulateTasksSchedulerDecorator m_strandAccumulateScheduler;

      private IExternalProxyScheduler m_strandProxyScheduler;
      private IExternalProxyScheduler m_concurrentProxyScheduler;

      private Task m_processTaskLoop;
      private IoServiceThreadPoolScheduler m_ioControlScheduler;
      private CancellationTokenSource m_stopCts;
      private TaskCompletionSource<object> m_completedTcs;
      private bool m_isDisposed;
      private IoServiceThreadPoolScheduler m_threadPoolScheduler;

      public InterleaveTaskSource(TaskScheduler controlScheduler, int maxTasksConcurrency)
      {
        init(controlScheduler, maxTasksConcurrency);
      }

      public IExternalProxyScheduler ConcurrentProxyScheduler
      {
        get
        {
          return m_concurrentProxyScheduler;
        }
      }

      public IExternalProxyScheduler StrandProxyScheduler
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

        m_maxConcurrentTaskBatch = Math.Min(checked(maxTasksConcurrency * CONCURRENCY_TASK_BATCH_MULTIPLIER), CONCURRENT_TASK_BATCH_LIMIT);
        m_exlusiveTaskAdded = new ThreadSafeSwitch();
        m_concurrentTaskAdded = new ThreadSafeSwitch();

        m_ownControlTaskScheduler = (controlScheduler == null);
        if (m_ownControlTaskScheduler)
        {
          var ioControlService = new IoServiceScheduler();
          m_ioControlScheduler = new IoServiceThreadPoolScheduler(ioControlService, CONTROL_SCHEDULER_CONCURRENCY);
          var controlProxyScheduler = new ExternalProxyScheduler(m_ioControlScheduler);
          m_controlTaskFactory = new TaskFactory(controlProxyScheduler.AsRealScheduler());
        }
        else
        {
          m_controlTaskFactory = new TaskFactory(controlScheduler);
        }


        var ioService = new IoServiceScheduler();
        m_threadPoolScheduler = new IoServiceThreadPoolScheduler(ioService, maxTasksConcurrency);
        m_concurrentAccumulateScheduler = new AccumulateTasksSchedulerDecorator(m_threadPoolScheduler, _ => taskAdded(m_concurrentTaskAdded));
        var strandScheduler = new StrandSchedulerDecorator(m_threadPoolScheduler);
        var innerStrandProxyScheduler = new ExternalProxyScheduler(strandScheduler);
        m_strandAccumulateScheduler = new AccumulateTasksSchedulerDecorator(strandScheduler, _ => taskAdded(m_exlusiveTaskAdded));
        m_strandProxyScheduler = new ExternalProxyScheduler(m_strandAccumulateScheduler);
        m_concurrentProxyScheduler = new ExternalProxyScheduler(m_concurrentAccumulateScheduler);
        m_processTaskLoop = null;
        m_completedTcs = new TaskCompletionSource<Object>();
        m_stopCts = new CancellationTokenSource();
        m_isDisposed = false;
      }

      private void taskAdded(ThreadSafeSwitch taskSwitch)
      {
        if (m_stopCts.IsCancellationRequested)
        {
          throw new InvalidOperationException("Could not add task - dispos in progress");
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
            var exclusiveQueueTasksParams = new QueueTasksParams(maxNumberOfQueuedtasks: MAX_STRAND_TASK_BATCH);
            exclusiveQueueResult = m_strandAccumulateScheduler.QueueAllTasksToInnerScheduler(exclusiveQueueTasksParams);
            await exclusiveQueueResult.WhenAllTask;

          } while (m_exlusiveTaskAdded.IsSet || exclusiveQueueResult.HasMoreTasks);

          do
          {
            m_concurrentTaskAdded.TryReset();
            var concurrentQueueTaskParams = new QueueTasksParams(maxNumberOfQueuedtasks: m_maxConcurrentTaskBatch);
            concurrentQueueResult = m_concurrentAccumulateScheduler.QueueAllTasksToInnerScheduler(concurrentQueueTaskParams);
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