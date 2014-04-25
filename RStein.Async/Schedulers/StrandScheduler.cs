//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace RStein.Async.Schedulers
//{
//  public class StrandSchedulerDecorator
//  {
//    private const int MAX_CONCURRENCY = 1;
//    private readonly System.Threading.Tasks.TaskScheduler m_originalScheduler;
    

//    private ConcurrentQueue<Task> m_tasks;

//    public StrandSchedulerDecorator(System.Threading.Tasks.TaskScheduler originalScheduler)
//    {
//      if (originalScheduler == null)
//      {
//        throw new ArgumentNullException("originalScheduler");
//      }
      
//      m_originalScheduler = originalScheduler;
//      m_tasks = new ConcurrentQueue<Task>();      
//    }

//    public virtual int MaximumConcurrencyLevel
//    {
//      get
//      {
//        return MAX_CONCURRENCY;
//      }
//    }


//    public override void QueueTask(Task task)
//    {
//      m_tasks.Enqueue(task);
//    }

//    public override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
//    {
//      m_tasks.Enqueue(task);
//      return false;

//    }


//    public override IEnumerable<Task> GetScheduledTasks()
//    {
//      return m_tasks.ToArray();
//    }
//  }
//}