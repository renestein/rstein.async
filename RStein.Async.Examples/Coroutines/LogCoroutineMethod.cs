using System;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.Async.Examples.Coroutines
{
  public class LogCoroutineMethod
  {
    public const int DEFAULT_DELAY_MS = 500;
    public const string ITERATION_MESSAGE_FORMAT = "Coroutine: {0,-20} iteration {1, -20} tid {2, -10}";
    public const string BEFORE_DELAY_MESSAGE_FORMAT = "Coroutine: {0,-20} before delay {1, -17} tid {2, -10}";
    public const string AFTER_DELAY_MESSAGE_FORMAT = "Coroutine: {0,-20} after delay {1, -18} tid {2, -10}";
    public const string BEFORE_YIELD_MESSAGE_FORMAT = "Coroutine: {0,-20} before yield {1, -17} tid {2, -10}";
    public const string AFTER_YIELD_MESSAGE_FORMAT = "Coroutine: {0,-20} after yield {1, -18} tid {2, -10}";
    public const string EXIT_MESSAGE_FORMAT = "Coroutine: {0, -20} work done.";
    private readonly string m_logCoroutineName;
    private readonly int m_numberOfIterations;

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

    public virtual async Task Start(Coroutine coroutine)
    {
      if (coroutine == null)
      {
        throw new ArgumentNullException("coroutine");
      }

      for (int i = 0; i < m_numberOfIterations; i++)
      {
        logMessage(ITERATION_MESSAGE_FORMAT, i);
        await coroutine;

        logMessage(BEFORE_DELAY_MESSAGE_FORMAT, i);
        await Task.Delay(DEFAULT_DELAY_MS);
        logMessage(AFTER_DELAY_MESSAGE_FORMAT, i);

        logMessage(BEFORE_YIELD_MESSAGE_FORMAT, i);
        await Task.Yield();
        logMessage(AFTER_YIELD_MESSAGE_FORMAT, i);
      }

      Console.WriteLine(EXIT_MESSAGE_FORMAT, m_logCoroutineName);
    }

    private void logMessage(string messageFormat, int iteration)
    {
      Console.WriteLine(messageFormat,
        m_logCoroutineName,
        iteration,
        Thread.CurrentThread.ManagedThreadId);
    }
  }
}