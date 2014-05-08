using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class NetThreadPoolSchedulerAdapter : TaskSchedulerBase
  {
    public override void QueueTask(Task task)
    {
      checkIfDisposed();
      createThreadPoolTask(task);
    }

    public override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      checkIfDisposed();
      if (taskWasPreviouslyQueued)
      {
        return false;
      }

      createThreadPoolTask(task);

      //TODO: Truth or lie?
      return true;
    }

    public override IEnumerable<Task> GetScheduledTasks()
    {
      checkIfDisposed();
      //TODO: Truth or lie?
      return Enumerable.Empty<Task>();
    }

    protected override void Dispose(bool disposing)
    {
      
    }

    private void createThreadPoolTask(Task originalTask)
    {
      var task = new Task(originalTask.RunSynchronously);
      task.Start(TaskScheduler.Default);
    }

    public override int MaximumConcurrencyLevel
    {
      get
      {
        return TaskScheduler.Default.MaximumConcurrencyLevel;
      }
    }
  }
}