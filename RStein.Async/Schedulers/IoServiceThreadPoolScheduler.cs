using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;

namespace RStein.Async.Schedulers
{
  public class IoServiceThreadPoolScheduler : TaskSchedulerBase
  {
    public const string POOL_THREAD_NAME_FORMAT = "IoServiceThreadPoolSchedulerThread#{0}";
    private const int EXPECTED_MIMINUM_THREADS = 1;

    private readonly IoServiceScheduler m_ioService;
    private List<Thread> m_threads;
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

      if (numberOfThreads < EXPECTED_MIMINUM_THREADS)
      {
        throw new ArgumentOutOfRangeException("numberOfThreads");
      }

      m_ioService = ioService;
      m_ioServiceWork = new Work(m_ioService);
      initThreads(numberOfThreads);

    }


    public override void QueueTask(Task task)
    {

      checkIfDisposed();
      m_ioService.QueueTask(task);

    }

    public override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      checkIfDisposed();
      return m_ioService.TryExecuteTaskInline(task, taskWasPreviouslyQueued);

    }

    public override IEnumerable<Task> GetScheduledTasks()
    {
      checkIfDisposed();
      return m_ioService.GetScheduledTasks();

    }

    public override int MaximumConcurrencyLevel
    {
      get
      {
        checkIfDisposed();
        return m_threads.Count();
      }

    }

    public override IExternalProxyScheduler ProxyScheduler
    {
      get
      {
        checkIfDisposed();
        return base.ProxyScheduler;
      }
      set
      {
        checkIfDisposed();
        m_ioService.ProxyScheduler = value;
        base.ProxyScheduler = value;
      }
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        m_ioServiceWork.Dispose();
        m_threads.ForEach(thread => thread.Join());
        m_ioService.Dispose();
      }
    }

    private void initThreads(int numberOfThreads)
    {
      m_threads = Enumerable.Range(0, numberOfThreads)
        .Select(threadNumber =>
                {
                  var poolThread = new Thread(() =>
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
                                    });

                  poolThread.IsBackground = true;
                  poolThread.Name = String.Format(POOL_THREAD_NAME_FORMAT, threadNumber);
                  return poolThread;

                }).ToList();

      m_threads.ForEach(thread => thread.Start());
    }

  }
}