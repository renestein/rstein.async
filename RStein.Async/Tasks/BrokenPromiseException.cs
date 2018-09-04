using System;
using System.Runtime.Serialization;

namespace RStein.Async.Tasks
{
  [Serializable]
  public class BrokenPromiseException : Exception
  {
    public BrokenPromiseException()
    {
    }
    public BrokenPromiseException(string message)
      : base(message)
    {
    }
    public BrokenPromiseException(string message, Exception inner)
      : base(message, inner)
    {
    }

    protected BrokenPromiseException(
      SerializationInfo info,
      StreamingContext context)
      : base(info, context)
    {
    }
  }
}