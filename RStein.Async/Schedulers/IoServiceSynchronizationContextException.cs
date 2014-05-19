using System;
using System.Runtime.Serialization;

namespace RStein.Async.Schedulers
{

  [Serializable]
  public class SynchronizationContextException : Exception
  {

    public SynchronizationContextException()
    {
    }
    public SynchronizationContextException(string message)
      : base(message)
    {

    }
    public SynchronizationContextException(string message, Exception inner)
      : base(message, inner)
    {

    }

    protected SynchronizationContextException(
      SerializationInfo info,
      StreamingContext context)
      : base(info, context)
    {
    }
  }
}