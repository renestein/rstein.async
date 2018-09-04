using System;
using System.Runtime.CompilerServices;
using RStein.Async.Schedulers;

namespace RStein.Async.Examples.Coroutines
{
  public class Coroutine : INotifyCompletion
  {
    private readonly IoServiceScheduler m_ioServiceScheduler;

    public Coroutine(IoServiceScheduler ioServiceScheduler)
    {
      m_ioServiceScheduler = ioServiceScheduler;
    }

    public virtual bool IsCompleted
    {
      get
      {
        return false;
      }
    }

    public void OnCompleted(Action continuation)
    {
      m_ioServiceScheduler.Post(continuation);
    }

    public virtual void Run()
    {
      m_ioServiceScheduler.Run();
    }

    public virtual Coroutine GetAwaiter()
    {
      return this;
    }

    public virtual void GetResult() {}
  }
}