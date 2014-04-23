using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class StrandSchedulerDecorator : TaskScheduler
  {
    private const int MAX_CONCURRENCY = 1;
    private readonly TaskScheduler m_originalScheduler;
    

    private ConcurrentQueue<Task> m_tasks;

    public StrandSchedulerDecorator(TaskScheduler originalScheduler)
    {
      if (originalScheduler == null)
      {
        throw new ArgumentNullException("originalScheduler");
      }
      
      m_originalScheduler = originalScheduler;
    }

    public override int MaximumConcurrencyLevel
    {
      get
      {
        return MAX_CONCURRENCY;
      }
    }


    protected override void QueueTask(Task task)
    {
      m_tasks.Enqueue(task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      m_tasks.Enqueue(task);
      return false;

    }
    

    protected override IEnumerable<Task> GetScheduledTasks()
    {
      return m_tasks.ToArray();
    }
  }
}