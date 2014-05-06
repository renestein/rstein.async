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
        return m_interleaveTaskSource.ConcurrentProxyScheduler;
      }
    }

    public IExternalProxyScheduler StrandProxyScheduler
    {
      get
      {
        return m_interleaveTaskSource.StrandProxyScheduler;
      }
    }

    public ITaskScheduler AsioStrandcheduler
    {
      get
      {
        return m_interleaveTaskSource.AsioStrandcheduler;
      }
    }
    public ITaskScheduler AsioConcurrentScheduler
    {
      get
      {
        return m_interleaveTaskSource.AsioConcurrentScheduler;
      }
    }


    public TaskScheduler ConcurrentScheduler
    {
      get
      {
        return m_interleaveTaskSource.ConcurrentProxyScheduler.AsRealScheduler();
      }
    }

    public TaskScheduler StrandScheduler
    {
      get
      {
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
    }

    private void Dispose(bool disposing)
    {
      if (disposing)
      {
        m_interleaveTaskSource.Dispose();
      }
    }

    private class InterleaveTaskSource
    {
      private const int CONTROL_SCHEDULER_CONCURRENCY = 1;

      private TaskFactory m_controlTaskFactory;
      private bool m_ownControlTaskScheduler;
      private AccumulateTasksSchedulerDecorator m_concurrentAccumulateScheduler;
      private AccumulateTasksSchedulerDecorator m_strandAccumulateScheduler;

      private IExternalProxyScheduler m_strandProxyScheduler;
      private IExternalProxyScheduler m_concurrentProxyScheduler;

      private Task m_processTasksLoop;
      private ThreadSafeSwitch m_taskAdded;
      private IoServiceThreadPoolScheduler m_ioControlScheduler;
      private CancellationTokenSource m_stopCts;
      private TaskCompletionSource<object> m_completedTcs;
      private bool m_isDisposed;

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
        var threadPoolScheduler = new IoServiceThreadPoolScheduler(ioService, maxTasksConcurrency);
        m_concurrentAccumulateScheduler = new AccumulateTasksSchedulerDecorator(threadPoolScheduler, taskAdded);
        var strandScheduler = new StrandSchedulerDecorator(threadPoolScheduler);
        m_strandAccumulateScheduler = new AccumulateTasksSchedulerDecorator(strandScheduler, taskAdded);
        m_strandProxyScheduler = new ExternalProxyScheduler(m_strandAccumulateScheduler);
        m_concurrentProxyScheduler = new ExternalProxyScheduler(m_concurrentAccumulateScheduler);
        m_processTasksLoop = null;
        m_taskAdded = new ThreadSafeSwitch();
        m_completedTcs = new TaskCompletionSource<Object>();
        m_stopCts = new CancellationTokenSource();
        m_isDisposed = false;
      }

      private void taskAdded(Task obj)
      {
        if (m_stopCts.IsCancellationRequested)
        {
          throw new InvalidOperationException("");
        }

        m_taskAdded.TrySet();
        isTaskLoopRequired();
      }

      private void isTaskLoopRequired()
      {
        if (m_taskAdded.IsSet && tryCreateLoopTask())
        {
          m_processTasksLoop.Start(m_controlTaskFactory.Scheduler);
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
        var task = Interlocked.CompareExchange(ref m_processTasksLoop, new Task(runInnerTaskLoop), null);
        return (task == null);
      }

      private bool tryResetLoopTask()
      {
        var currentTask = m_processTasksLoop;
        var task = Interlocked.CompareExchange(ref m_processTasksLoop, null, currentTask);
        return (task != null);
      }

      private async void runInnerTaskLoop()
      {
        do
        {
          m_taskAdded.TryReset();

          var quuedTaskPair = m_strandAccumulateScheduler.QueueAllTasksToInnerScheduler();

          while (quuedTaskPair.Item1 > 0)
          {
            await quuedTaskPair.Item2;
          }

          await m_concurrentAccumulateScheduler.QueueAllTasksToInnerScheduler().Item2;

        } while (m_taskAdded.IsSet);

        bool resetTaskResult = tryResetLoopTask();
        Debug.Assert(resetTaskResult);
        isTaskLoopRequired();
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
          waitForCompletion();
          m_concurrentAccumulateScheduler.Dispose();
          m_strandAccumulateScheduler.Dispose();
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