using System.Globalization;
using System.Linq;
using RStein.Async.Schedulers;

namespace RStein.Async.Examples.Coroutines
{
  public class LogCoroutineTester
  {
    private readonly IoServiceScheduler m_scheduler;
    private readonly Coroutine m_coroutine;
    private ExternalProxyScheduler m_proxyScheduler;

    public LogCoroutineTester()
    {
      m_scheduler = new IoServiceScheduler();
      m_proxyScheduler = new ExternalProxyScheduler(m_scheduler);
      m_coroutine = new Coroutine(m_scheduler);
    }

    public void Start()
    {
      addCoroutineMethods();
      m_coroutine.Run();
    }

    private void addCoroutineMethods()
    {
      const int NUMBER_OF_COROUTINES = 200;
       const int NUMBER_OF_ITERATIONS = 100;

      Enumerable.Range(0, NUMBER_OF_COROUTINES)
        .Select(i => m_scheduler.Post(() => new LogCoroutineMethod(NUMBER_OF_ITERATIONS, i.ToString(CultureInfo.InvariantCulture))
                                            .Start(m_coroutine))).ToArray();
    }
  }
}