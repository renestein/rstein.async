using System;
using System.Threading;

namespace RStein.Async.Schedulers
{
  public sealed class Work : IDisposable
  {
    private readonly CancellationTokenSource m_cancelTokenSource;
    
    public Work(IoServiceScheduler scheduler)
    {
      m_cancelTokenSource = new CancellationTokenSource();
      scheduler.AddWork(this);
    }
    
    internal CancellationToken CancelToken => m_cancelTokenSource.Token;

    internal void RegisterWorkDisposedHandler(Action action) => m_cancelTokenSource.Token.Register(action);

    public void Dispose() => Dispose(true);

    private void Dispose(bool disposing)
    {
      if (disposing)
      {
        m_cancelTokenSource.Cancel();        
      }
    }
  }
}