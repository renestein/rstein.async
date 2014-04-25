using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class IoServiceThreadPoolScheduler : ITaskScheduler
  {
    private readonly IoServiceScheduler m_ioService;
    private IEnumerable<Thread> m_threads;
    private readonly Work m_ioServiceWork;

    public IoServiceThreadPoolScheduler(IoServiceScheduler ioService) 
      : this(ioService, Environment.ProcessorCount)
    {
     
    }

    public IoServiceThreadPoolScheduler(IoServiceScheduler ioService, int numberOfThreads)
    {
      if (ioService == null)
      {
        throw new ArgumentNullException("ioService");
      }
      
      if (numberOfThreads < 1)
      {
        throw  new ArgumentOutOfRangeException("numberOfThreads");
      }

      m_ioService = ioService;
      m_ioServiceWork = new Work(m_ioService);
      initThreads(numberOfThreads);
    }


    public virtual void QueueTask(Task task)
    {
      m_ioService.QueueTask(task);
    }

    public virtual bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      return m_ioService.TryExecuteTaskInline(task, taskWasPreviouslyQueued);
    }

    public virtual IEnumerable<Task> GetScheduledTasks()
    {
      return m_ioService.GetScheduledTasks();
    }

    public virtual int MaximumConcurrencyLevel
    {
      get
      {
        return m_threads.Count();
      }
      
    }

    public virtual void SetProxyScheduler(IExternalProxyScheduler scheduler)
    {
      m_ioService.SetProxyScheduler(scheduler);
    }

    public void Dispose()
    {
      m_ioServiceWork.Dispose();
      foreach (var thread in m_threads)
      {
        thread.Join();
      }
    }

    private void initThreads(int numberOfThreads)
    {
      m_threads = Enumerable.Range(0, numberOfThreads)
        .Select(threadNumber => new Thread(() =>
                                           {
                                             try
                                             {
                                               m_ioService.Run();
                                             }
                                             catch (Exception ex)
                                             {
                                               Trace.WriteLine(ex);
                                               Environment.FailFast(null, ex);
                                             }
                                           })).ToList();
    }
  }
}