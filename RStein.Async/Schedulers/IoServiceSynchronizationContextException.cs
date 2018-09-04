using System;

namespace RStein.Async.Schedulers
{
  public class SynchronizationContextException : Exception
  {
    public SynchronizationContextException() {}

    public SynchronizationContextException(string message)
      : base(message) {}

    public SynchronizationContextException(string message, Exception inner)
      : base(message, inner) {}

  }
}