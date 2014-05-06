using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RStein.Async.Threading;

namespace RStein.Async.Schedulers
{
  public class ConcurrentStrandSchedulerPair
  {
    public ConcurrentStrandSchedulerPair(int maxTasksConcurrency)
    {
    }

    private class InterleaveTaskSource
    {
      private AccumulateTasksSchedulerDecorator m_accumulateTasksConcurrentScheduler;
      private AccumulateTasksSchedulerDecorator m_accumulateTasksStrandScheduler;
      private IExternalProxyScheduler m_strandProxyScheduler;
      private IExternalProxyScheduler m_concurrentProxyScheduler;
      private readonly ThreadSafeSwitch m_strandTasksInProgesssSwitch;
      private readonly ThreadSafeSwitch m_concurrentTasksInProgesssSwitch;
      private int m_concurrentTasksInProgress;
      private int m_strandTasksInProgress;

      public InterleaveTaskSource(int maxTasksConcurrency)
      {
        var ioService = new IoServiceScheduler();
        var threadPoolScheduler = new IoServiceThreadPoolScheduler(ioService, maxTasksConcurrency);
        var strandScheduler = new StrandSchedulerDecorator(threadPoolScheduler);
        m_accumulateTasksStrandScheduler = new AccumulateTasksSchedulerDecorator(strandScheduler, newStrandTask);
        m_accumulateTasksConcurrentScheduler = new AccumulateTasksSchedulerDecorator(threadPoolScheduler, newConcurrentTask);
        m_strandProxyScheduler = new ExternalProxyScheduler(m_accumulateTasksStrandScheduler);
        m_concurrentProxyScheduler = new ExternalProxyScheduler(m_accumulateTasksConcurrentScheduler);
        m_strandTasksInProgesssSwitch = new ThreadSafeSwitch();
        tryBeginProcessingConcurrentTasks();
      }

      private void newConcurrentTask(Task obj)
      {
        m_concurrentTasksInProgesssSwitch.TrySet();
      }

      private void tryBeginProcessingConcurrentTasks()
      {
        if (m_strandTasksInProgesssSwitch.IsSet)
        {
          return;
        }

        m_accumulateTasksConcurrentScheduler.QueueAllTasksToInnerScheduler(incrementConcurrentTasksCount, decrementConcurrentTasksCount);
      }

      private void decrementConcurrentTasksCount(Task obj)
      {
        int currentValue = Interlocked.Decrement(ref m_concurrentTasksInProgress);
        if ((currentValue == 0) && (m_strandTasksInProgesssSwitch.IsSet))
        {
          tryBeginProcessingStrandTasks();
        }
      }

      private void tryBeginProcessingStrandTasks()
      {

        if (isConcurrentTaskInProgress())
        {
          return;
        }

        m_accumulateTasksStrandScheduler.QueueAllTasksToInnerScheduler(incrementStrandTasksCount, decrementStrandTasksCount);
      }

      private void newStrandTask(Task obj)
      {
        bool setNow = m_strandTasksInProgesssSwitch.TrySet();
        if (setNow)
        {
          tryBeginProcessingStrandTasks();
        }
      }

      private void decrementStrandTasksCount(Task obj)
      {
        int newValue = Interlocked.Decrement(ref m_strandTasksInProgress);

        if (newValue == 0)
        {
          bool resetNow = m_strandTasksInProgesssSwitch.TryReset();
          Debug.Assert(resetNow);
          tryBeginProcessingConcurrentTasks();
        }
      }

      private void incrementStrandTasksCount(Task obj)
      {
        Interlocked.Decrement(ref m_strandTasksInProgress);
      }

      private bool isConcurrentTaskInProgress()
      {
        return m_concurrentTasksInProgress > 0;
      }

      private void incrementConcurrentTasksCount(Task obj)
      {
        Interlocked.Increment(ref m_concurrentTasksInProgress);
      }
    }
  }
}