using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class ExternalProxyScheduler : TaskScheduler, IExternalProxyScheduler, IDisposable
  {
    private readonly ITaskScheduler m_realScheduler;

    public ExternalProxyScheduler(ITaskScheduler realScheduler)
    {
      if (realScheduler == null)
      {
        throw new ArgumentNullException("realScheduler");
      }

      m_realScheduler = realScheduler;
      m_realScheduler.ProxyScheduler = this;
    }

    public virtual bool DoTryExecuteTask(Task task)
    {
      return TryExecuteTask(task);
    }

    public virtual TaskScheduler AsRealScheduler()
    {
      return this;
    }

    public void Dispose()
    {
      Dispose(true);
    }

    public override int MaximumConcurrencyLevel
    {
      get
      {
        return m_realScheduler.MaximumConcurrencyLevel;
      }
    }

    protected override void QueueTask(Task task)
    {
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