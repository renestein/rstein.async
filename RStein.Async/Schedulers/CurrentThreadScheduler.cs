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
        checkIfDisposed();
        return MAXIMUM_CONCURRENCY_LEVEL;
      }
    }

    public override void QueueTask(Task task)
    {
      checkIfDisposed();
      ProxyScheduler.DoTryExecuteTask(task);
    }

    public override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      checkIfDisposed();
      ProxyScheduler.DoTryExecuteTask(task);
      return true;
    }

    public override IEnumerable<Task> GetScheduledTasks()
    {
      checkIfDisposed();
      return Enumerable.Empty<Task>();
    }

    protected override void Dispose(bool disposing)
    {
    }
  }
}