using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public interface ITaskScheduler : IDisposable
  {
    void QueueTask(Task task);
    bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued);
    IEnumerable<Task> GetScheduledTasks();
    int MaximumconcurrencyLevel{get;}
    void SetProxyScheduler(IExternalProxyScheduler scheduler);
  }
}