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
    private const int EXPECTED_MIMINUM_THREADS = 1;
    private readonly IoServiceScheduler m_ioService;

    private List<Thread> m_threads;
    private readonly Work m_ioServiceWork;
    private readonly object m_schedulerLock;
    private bool m_disposed;
    private readonly CancellationTokenSource m_schedulerCanContinue;

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

      if (numberOfThreads < EXPECTED_MIMINUM_THREADS)
      {
        throw new ArgumentOutOfRangeException("numberOfThreads");
      }

      m_schedulerLock = new Object();
      m_ioService = ioService;
      m_ioServiceWork = new Work(m_ioService);
      initThreads(numberOfThreads);
      m_schedulerCanContinue = new CancellationTokenSource();
      m_disposed = false;

    }


    public virtual void QueueTask(Task task)
    {
      
        checkIfDisposed();
        m_ioService.QueueTask(task);
      
    }

    public virtual bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        checkIfDisposed();
        return m_ioService.TryExecuteTaskInline(task, taskWasPreviouslyQueued);

    }

    public virtual IEnumerable<Task> GetScheduledTasks()
    {
        checkIfDisposed();
        return m_ioService.GetScheduledTasks();

    }

    public virtual int MaximumConcurrencyLevel
    {
      get
      {
          checkIfDisposed();
          return m_threads.Count();        
      }

    }

    public virtual void SetProxyScheduler(IExternalProxyScheduler scheduler)
    {
        checkIfDisposed();
        m_ioService.SetProxyScheduler(scheduler);

    }

    public void Dispose()
    {
      m_schedulerCanContinue.Cancel();
      lock (m_schedulerLock)
      {

        if (m_disposed)
        {
          return;
        }

        m_ioServiceWork.Dispose();
        m_threads.ForEach(thread => thread.Join());
        m_ioService.Dispose();
        m_disposed = true;
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

      m_threads.ForEach(thread => thread.Start());
    }

    private void checkIfDisposed()
    {
      if (m_disposed)
      {
        throw new ObjectDisposedException(GetType().FullName);
      }
    }
  }
}