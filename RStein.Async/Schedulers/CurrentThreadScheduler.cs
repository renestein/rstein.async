using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class CurrentThreadScheduler : TaskSchedulerBase
  {
    private const int MAXIMUM_CONCURRENCY_LEVEL = 1;
    public override int MaximumConcurrencyLevel
    {
      get
      {
        CheckIfDisposed();
        return MAXIMUM_CONCURRENCY_LEVEL;
      }
    }

    public override void QueueTask(Task task)
    {
      CheckIfDisposed();
      ProxyScheduler.DoTryExecuteTask(task);
    }

    public override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      CheckIfDisposed();
      ProxyScheduler.DoTryExecuteTask(task);
      return true;
    }

    public override IEnumerable<Task> GetScheduledTasks()
    {
      CheckIfDisposed();
      return Enumerable.Empty<Task>();
    }

    protected override void Dispose(bool disposing)
    {
    }
  }
}