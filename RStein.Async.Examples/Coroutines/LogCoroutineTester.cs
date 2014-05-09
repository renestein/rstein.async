using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using RStein.Async.Schedulers;

namespace RStein.Async.Examples.Coroutines
{
  public class LogCoroutineTester : IDisposable
  {
    private readonly IoServiceScheduler m_scheduler;
    private readonly Coroutine m_coroutine;
    private ExternalProxyScheduler m_proxyScheduler;
    private Work m_work;

    public LogCoroutineTester()
    {
      m_scheduler = new IoServiceScheduler();
      m_proxyScheduler = new ExternalProxyScheduler(m_scheduler);
      m_coroutine = new Coroutine(m_scheduler);

    }

    public void Start()
    {
      m_work = new Work(m_scheduler);
      addCoroutineMethods();
      m_coroutine.Run();
    }

    private void addCoroutineMethods()
    {
      const int NUMBER_OF_COROUTINES = 10;
      const int NUMBER_OF_ITERATIONS = 10;

      var tasksArray = Enumerable.Range(0, NUMBER_OF_COROUTINES)
                      .Select(i => m_scheduler.Post(() =>
                                                      new LogCoroutineMethod(NUMBER_OF_ITERATIONS, i.ToString(CultureInfo.InvariantCulture))
                                                     .Start(m_coroutine)))
                      .ToArray();

      m_scheduler.Post(async () =>
                             {
                               await Task.WhenAll(tasksArray);
                               Console.WriteLine("All coroutines finished!");
                               m_work.Dispose();
                             });

    }

    public void Dispose()
    {
      Dispose(false);
    }

    protected void Dispose(bool disposing)
    {
      if (disposing)
      {
        m_scheduler.Dispose();
      }
    }
  }
}