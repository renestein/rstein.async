using System;
using System.Threading.Tasks;
using RStein.Async.Schedulers;
using RStein.Async.Tasks;
using RStein.Async.Threading;

namespace RStein.Async.ConsoleEx
{
  public class ConsoleRunner
  {
    private readonly IoServiceScheduler m_scheduler;
    private readonly LogSynchronizationContextDecorator m_synchContext;

    public ConsoleRunner()
    {
      m_scheduler = new IoServiceScheduler();
      var proxyScheduler = new ProxyScheduler(m_scheduler);
      m_synchContext = new LogSynchronizationContextDecorator(
                              new IoServiceSynchronizationContext(m_scheduler, disposeIoServiceAfterComplete: true));
    }

    public static void Run(Func<Task> function)
    {
      var runner = getConsoleRunner();
      runner.startInner(() => runner.m_scheduler.Post(function));
    }

    public static TResult Run<TResult>(Func<TResult> function)
    {
      var runner = getConsoleRunner();
      var result = default(TResult);
      runner.startInner(() => runner.m_scheduler.Post(() => result = function()));
      return result;
    }

    public static TResult Run<TResult>(Func<Task<TResult>> function)
    {
      var runner = getConsoleRunner();
      var result = default(TResult);
      runner.startInner(() => runner.m_scheduler.Post(async () => result = await function()));
      return result;
    }

    public static void Run(Action function)
    {
      var runner = getConsoleRunner();
      runner.startInner(() => runner.m_scheduler.Post(function));
    }

    private static ConsoleRunner getConsoleRunner()
    {
      var runner = new ConsoleRunner();
      return runner;
    }


    private void startInner(Func<Task> schedulerAction)
    {
      using (new ScopedSynchronizationContext(m_synchContext))
      {
        m_synchContext.OperationStarted();
        var actionTask = schedulerAction();
        actionTask.ContinueWith(_ => m_synchContext.OperationCompleted(), TaskScheduler.Default);
        m_scheduler.Run();
        actionTask.WaitAndPropagateException();
      }
    }
  }
}