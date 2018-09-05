using System;
using System.Threading;

namespace RStein.Async.Schedulers
{
  public class LogSynchronizationContextDecorator : SynchronizationContext
  {
    private readonly SynchronizationContext _innerContext;

    public LogSynchronizationContextDecorator(SynchronizationContext innerContext)
    {
      _innerContext = innerContext;
      if (innerContext == null)
      {
        throw new ArgumentNullException(nameof(innerContext));
      }
    }

    public override void Send(SendOrPostCallback d, object state)
    {
      Console.WriteLine($"{nameof(LogSynchronizationContextDecorator)}--{nameof(Send)}");
      _innerContext.Send(d, state);
    }

    public override void Post(SendOrPostCallback d, object state)
    {
      Console.WriteLine($"{nameof(LogSynchronizationContextDecorator)}--{nameof(Post)}");
      _innerContext.Post(d, state);
    }

    public override void OperationStarted()
    {
      Console.WriteLine($"{nameof(LogSynchronizationContextDecorator)}--{nameof(OperationStarted)}");
      _innerContext.OperationStarted();
    }

    public override void OperationCompleted()
    {
      Console.WriteLine($"{nameof(LogSynchronizationContextDecorator)}--{nameof(OperationCompleted)}");
      _innerContext.OperationCompleted();
    }
  }
}