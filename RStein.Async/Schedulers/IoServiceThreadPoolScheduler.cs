using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class IoServiceThreadPoolScheduler : TaskSchedulerBase
  {
    public const string POOL_THREAD_NAME_FORMAT = "IoServiceThreadPoolSchedulerThread#{0}";
    private const int EXPECTED_MININUM_THREADS = 1;

    private readonly IoServiceScheduler m_ioService;
    private readonly Work m_ioServiceWork;
    private List<Thread> m_threads;

    public IoServiceThreadPoolScheduler(IoServiceScheduler ioService)
      : this(ioService, Environment.ProcessorCount)
    {
    }

    public IoServiceThreadPoolScheduler(IoServiceScheduler ioService, int numberOfThreads)
    {
      if (numberOfThreads < EXPECTED_MININUM_THREADS)
      {
        throw new ArgumentOutOfRangeException(nameof(numberOfThreads));
      }

      m_ioService = ioService ?? throw new ArgumentNullException(nameof(ioService));
      m_ioServiceWork = new Work(m_ioService);
      initThreads(numberOfThreads);
    }

    public override int MaximumConcurrencyLevel
    {
      get
      {
        CheckIfDisposed();
        return m_threads.Count;
      }
    }

    public override IProxyScheduler ProxyScheduler
    {
      get
      {
        CheckIfDisposed();
        return base.ProxyScheduler;
      }
      set
      {
        CheckIfDisposed();
        m_ioService.ProxyScheduler = value;
        base.ProxyScheduler = value;
      }
    }


    public override void QueueTask(Task task)
    {
      CheckIfDisposed();
      m_ioService.QueueTask(task);
    }

    public override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      CheckIfDisposed();
      return m_ioService.TryExecuteTaskInline(task, taskWasPreviouslyQueued);
    }

    public override IEnumerable<Task> GetScheduledTasks()
    {
      CheckIfDisposed();
      return m_ioService.GetScheduledTasks();
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
                      if (Debugger.IsAttached)
                      {
                        Debugger.Break();
                      }
                      else
                      {
                        Environment.FailFast(null, ex);
                      }
                    }
                  })
                  {
                    IsBackground = true,
                    Name = String.Format(POOL_THREAD_NAME_FORMAT, threadNumber)};

                  return poolThread;
                }).ToList();

      m_threads.ForEach(thread => thread.Start());
    }
  }
}