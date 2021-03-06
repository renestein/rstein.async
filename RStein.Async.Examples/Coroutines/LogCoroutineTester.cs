﻿using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using RStein.Async.Schedulers;

namespace RStein.Async.Examples.Coroutines
{
  public class LogCoroutineTester : IDisposable
  {
    private readonly Coroutine m_coroutine;
    private readonly IoServiceScheduler m_scheduler;
    private ProxyScheduler m_proxyScheduler;
    private Work m_work;

    public LogCoroutineTester()
    {
      m_scheduler = new IoServiceScheduler();
      m_proxyScheduler = new ProxyScheduler(m_scheduler);
      m_coroutine = new Coroutine(m_scheduler);
    }

    public void Dispose()
    {
      Dispose(false);
    }

    public void Start()
    {
      m_work = new Work(m_scheduler);
      addCoroutineMethods();
      m_coroutine.Run();
    }

    protected void Dispose(bool disposing)
    {
      if (disposing)
      {
        m_scheduler.Dispose();
      }
    }

    private void addCoroutineMethods()
    {
      const int NUMBER_OF_COROUTINES = 30;
      const int NUMBER_OF_ITERATIONS = 40;

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
  }
}