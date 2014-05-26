using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public interface ITaskScheduler : IDisposable
  {
    int MaximumConcurrencyLevel
    {
      get;
    }

    IProxyScheduler ProxyScheduler
    {
      get;
      set;
    }

    Task Complete
    {
      get;
    }
    void QueueTask(Task task);
    bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued);
    IEnumerable<Task> GetScheduledTasks();
  }
}