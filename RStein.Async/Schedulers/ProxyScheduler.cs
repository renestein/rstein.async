using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class ProxyScheduler : TaskScheduler, IProxyScheduler, IDisposable
  {
    private readonly ITaskScheduler m_realScheduler;

    public ProxyScheduler(ITaskScheduler realScheduler)
    {
      m_realScheduler = realScheduler ?? throw new ArgumentNullException(nameof(realScheduler));
      m_realScheduler.ProxyScheduler = this;
    }

    public override int MaximumConcurrencyLevel => m_realScheduler.MaximumConcurrencyLevel;

    public void Dispose()
    {
      Dispose(true);
    }

    public virtual bool DoTryExecuteTask(Task task)
    {
      if (task == null)
      {
        throw new ArgumentNullException(nameof(task));
      }

      var taskExecuted = TryExecuteTask(task);

      if (taskExecuted)
      {
        task.RemoveProxyScheduler();
      }

      return taskExecuted;
    }

    public virtual TaskScheduler AsTplScheduler()
    {
      return this;
    }

    protected override void QueueTask(Task task)
    {
      task.SetProxyScheduler(this);
      m_realScheduler.QueueTask(task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      if (!taskWasPreviouslyQueued)
      {
        task.SetProxyScheduler(this);
      }

      return m_realScheduler.TryExecuteTaskInline(task, taskWasPreviouslyQueued);
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
      return m_realScheduler.GetScheduledTasks();
    }


    protected void Dispose(bool disposing)
    {
      if (disposing)
      {
        m_realScheduler.Dispose();
      }
    }
  }
}