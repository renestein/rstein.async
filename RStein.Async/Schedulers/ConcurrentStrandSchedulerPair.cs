using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RStein.Async.Threading;

namespace RStein.Async.Schedulers
{
  public class ConcurrentStrandSchedulerPair
  {
    private readonly InterleaveTaskSource m_interleaveTaskSource;

    public ConcurrentStrandSchedulerPair(int maxTasksConcurrency)
      : this(null, maxTasksConcurrency)
    {

    }

    public ConcurrentStrandSchedulerPair(TaskScheduler controlScheduler, int maxTasksConcurrency)
    {
      m_interleaveTaskSource = new InterleaveTaskSource(controlScheduler, maxTasksConcurrency);

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

    private class InterleaveTaskSource
    {
      private const int CONTROL_SCHEDULER_CONCURRENCY = 1;

      private TaskFactory m_controlTaskFactory;
      private AccumulateTasksSchedulerDecorator m_concurrentAccumulateScheduler;
      private AccumulateTasksSchedulerDecorator m_strandAccumulateScheduler;

      private IExternalProxyScheduler m_strandProxyScheduler;
      private IExternalProxyScheduler m_concurrentProxyScheduler;

      private Task m_processTasksLoop;
      private ThreadSafeSwitch m_taskAdded;


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

      private void init(TaskScheduler controlScheduler, int maxTasksConcurrency)
      {
        if (controlScheduler == null)
        {
          var ioControlService = new IoServiceScheduler();
          var ioControlScheduler = new IoServiceThreadPoolScheduler(ioControlService, CONTROL_SCHEDULER_CONCURRENCY);
          var controlProxyScheduler = new ExternalProxyScheduler(ioControlScheduler);
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
        m_strandProxyScheduler = new ExternalProxyScheduler(m_concurrentAccumulateScheduler);
        m_concurrentProxyScheduler = new ExternalProxyScheduler(m_concurrentAccumulateScheduler);
        m_processTasksLoop = null;
        m_taskAdded = new ThreadSafeSwitch();
      }

      private void taskAdded(Task obj)
      {
        m_taskAdded.TrySet();
        isTaskLoopRequired();
      }

      private void isTaskLoopRequired()
      {
        if (m_taskAdded.IsSet && tryCreateLoopTask())
        {
          m_processTasksLoop.Start(m_controlTaskFactory.Scheduler);
        }
      }

      private bool tryCreateLoopTask()
      {
        var task = Interlocked.CompareExchange(ref m_processTasksLoop, new Task(runInnerTaskLoop), null);
        return (task != null);
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
    }
  }
}