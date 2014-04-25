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
    private readonly IoServiceScheduler m_innerScheduler;
    private IEnumerable<Thread> m_threads;
    private readonly Work m_ioServiceWork;
    private IExternalProxyScheduler m_proxyScheduler;

    public IoServiceThreadPoolScheduler(IoServiceScheduler innerScheduler, IExternalProxyScheduler proxyScheduler) 
      : this(innerScheduler, Environment.ProcessorCount, proxyScheduler)
    {
     
    }

    public IoServiceThreadPoolScheduler(IoServiceScheduler innerScheduler, int numberOfThreads, IExternalProxyScheduler proxyScheduler)
    {
      if (innerScheduler == null)
      {
        throw new ArgumentNullException("innerScheduler");
      }
      
      if (numberOfThreads < 1)
      {
        throw  new ArgumentOutOfRangeException("numberOfThreads");
      }

      m_innerScheduler = innerScheduler;
      m_proxyScheduler = proxyScheduler;
      m_ioServiceWork = new Work(m_innerScheduler);
      initThreads(numberOfThreads);
    }


    public virtual void QueueTask(Task task)
    {
      m_innerScheduler.QueueTask(task);
    }

    public bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      return m_innerScheduler.TryExecuteTaskInline(task, taskWasPreviouslyQueued);
    }

    public IEnumerable<Task> GetScheduledTasks()
    {
      return m_innerScheduler.GetScheduledTasks();
    }

    public int MaximumConcurrencyLevel
    {
      get
      {
        return m_threads.Count();
      }
      
    }

    public void SetProxyScheduler(IExternalProxyScheduler scheduler)
    {
      m_innerScheduler.SetProxyScheduler(scheduler);
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
                                               m_innerScheduler.Run();
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