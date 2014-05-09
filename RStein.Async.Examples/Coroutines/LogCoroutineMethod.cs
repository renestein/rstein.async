using System;
using System.Diagnostics;
using System.Threading;

namespace RStein.Async.Examples.Coroutines
{
  public class LogCoroutineMethod
  {
    public const string ITERATION_MESSAGE_FORMAT = "{0,-4} iteration {1, -4} tid {2, -4}";
    public const string EXIT_MESSAGE_FORMAT = "{0}  - work done.";
    private readonly int m_numberOfIterations;
    private readonly string m_logCoroutineName;

    public LogCoroutineMethod(int numberOfIterations, string logCoroutineName)
    {
      if (numberOfIterations <= 0)
      {
        throw new ArgumentOutOfRangeException("numberOfIterations");
      }

      if (String.IsNullOrEmpty(logCoroutineName))
      {
        throw new ArgumentException("logCoroutineName");
      }

      m_numberOfIterations = numberOfIterations;
      m_logCoroutineName = logCoroutineName;
    }

    public virtual async void Start(Coroutine coroutine)
    {
      if (coroutine == null)
      {
        throw new ArgumentNullException("coroutine");
      }

      for (int i = 0; i < m_numberOfIterations; i++)
      {
        Console.WriteLine(ITERATION_MESSAGE_FORMAT, m_logCoroutineName, i, Thread.CurrentThread.ManagedThreadId);
        await coroutine;
      }
      Console.WriteLine(EXIT_MESSAGE_FORMAT, m_logCoroutineName);

    }
  }
}