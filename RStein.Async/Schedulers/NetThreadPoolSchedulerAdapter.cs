using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class NetThreadPoolSchedulerAdapter : TaskSchedulerBase
  {
    public override int MaximumConcurrencyLevel
    {
      get
      {
        return TaskScheduler.Default.MaximumConcurrencyLevel;
      }
    }

    public override void QueueTask(Task task)
    {
      checkIfDisposed();
      createThreadPoolTask(task);
    }

    public override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      checkIfDisposed();
      return false;
    }

    public override IEnumerable<Task> GetScheduledTasks()
    {
      checkIfDisposed();
      //TODO: Truth or lie?
      return Enumerable.Empty<Task>();
    }

    protected override void Dispose(bool disposing) {}

    private void createThreadPoolTask(Task originalTask)
    {
      Debug.Assert(ProxyScheduler != null);
      var threadPoolTask = new Task<bool>(originalTask.RunOnProxyScheduler);
      threadPoolTask.Start(TaskScheduler.Default);
    }
  }
}