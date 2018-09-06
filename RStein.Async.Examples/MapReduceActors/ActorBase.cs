using System;
using System.Threading.Tasks;

namespace RStein.Async.Examples.MapReduceActors
{
  public abstract class ActorBase : IActor
  {
    private readonly TaskCompletionSource<Object> m_completedTaskTcs;
    private int m_completeCountDown;
    private bool m_completed;

    protected ActorBase(int completeCountDown = 1)
    {
      m_completeCountDown = completeCountDown;
      m_completedTaskTcs = new TaskCompletionSource<object>();
      m_completed = false;
    }

    public virtual void Complete()
    {
      if (m_completed)
      {
        return;
      }

      if (--m_completeCountDown != 0)
      {
        return;
      }

      try
      {
        m_completed = true;
        DoInnerComplete();
      }
      catch (Exception ex)
      {
        m_completedTaskTcs.SetException(ex);
      }

      m_completedTaskTcs.SetResult(true);
    }

    public Task Completed => m_completedTaskTcs.Task;

    protected virtual void DoInnerComplete() {}
  }
}