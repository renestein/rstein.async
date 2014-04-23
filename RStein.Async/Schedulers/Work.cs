using System;
using System.Threading;

namespace RStein.Async.Schedulers
{
  public class Work : IDisposable
  {
    private CancellationTokenSource m_cancelTokenSource;
    
    public Work(IoServiceScheduler scheduler)
    {
      m_cancelTokenSource = new CancellationTokenSource();      
    }

    public CancellationToken CancelToken
    {
      get
      {
        return m_cancelTokenSource.Token;
      }
    }

    public void Dispose()
    {
      m_cancelTokenSource.Cancel();
    }
  }
}