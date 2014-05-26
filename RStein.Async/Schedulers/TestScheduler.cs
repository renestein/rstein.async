using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class TestScheduler : TaskSchedulerBase
  {
    private const string FORMAT_STRING = "TestScheduler: Scheduled tasks: {0}";
    private readonly IoServiceScheduler m_ioServiceScheduler;
    private readonly SynchronizationContext m_synchronizationContext;
    private readonly TaskFactory m_taskFactory;

    public TestScheduler()
    {
      m_ioServiceScheduler = new IoServiceScheduler();
      var scheduler = new ProxyScheduler(this);
      m_taskFactory = new TaskFactory(scheduler.AsTplScheduler());
      m_synchronizationContext = new IoServiceSynchronizationContext(m_ioServiceScheduler);
    }

    public virtual SynchronizationContext SynchronizationContext
    {
      get
      {
        return m_synchronizationContext;
      }
    }

    public override int MaximumConcurrencyLevel
    {
      get
      {
        return m_ioServiceScheduler.MaximumConcurrencyLevel;
      }
    }

    public override IProxyScheduler ProxyScheduler
    {
      get
      {
        return base.ProxyScheduler;
      }
      set
      {
        base.ProxyScheduler = value;
        m_ioServiceScheduler.ProxyScheduler = value;
      }
    }

    public virtual TaskFactory TaskFactory
    {
      get
      {
        return m_taskFactory;
      }
    }

    public virtual int GetScheduledTaskCount()
    {
      return GetScheduledTasks().Count();
    }

    public virtual int RunOneTask()
    {
      assertTaskExists();
      return m_ioServiceScheduler.RunOne();
    }

    public virtual int RunAllTasks()
    {
      assertTaskExists();
      return m_ioServiceScheduler.Poll();
    }

    public virtual int RunTasks(int maxTasksCount)
    {
      if (maxTasksCount <= 0)
      {
        return 0;
      }

      int tasksRemaining = maxTasksCount;
      int tasksExecuted = 0;
      bool lastTaskExecuted = false;

      do
      {
        lastTaskExecuted = tryExecuteNextTask();
        tasksRemaining--;

        if (lastTaskExecuted)
        {
          tasksExecuted++;
        }
      } while (tasksRemaining > 0 && lastTaskExecuted);

      return tasksExecuted;
    }

    public override void QueueTask(Task task)
    {
      checkIfDisposed();
      m_ioServiceScheduler.QueueTask(task);
    }

    public override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      checkIfDisposed();
      return m_ioServiceScheduler.TryExecuteTaskInline(task, taskWasPreviouslyQueued);
    }

    public override IEnumerable<Task> GetScheduledTasks()
    {
      return m_ioServiceScheduler.GetScheduledTasks();
    }


    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        m_ioServiceScheduler.Dispose();
      }
    }

    private void assertTaskExists()
    {
      Debug.Assert(GetScheduledTaskCount() > 0);
    }

    private bool tryExecuteNextTask()
    {
      int taskCount = m_ioServiceScheduler.PollOne();
      return (taskCount == IoServiceScheduler.POLLONE_RUNONE_MAX_TASKS);
    }

    public override string ToString()
    {
      return string.Format(FORMAT_STRING, GetScheduledTaskCount());
    }
  }
}