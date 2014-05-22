using System;
using System.Threading.Tasks;

namespace RStein.Async.Tasks
{
  public static class PredefinedTasks
  {
    private static readonly TaskCompletionSource<Object> m_completedTcs;
    private static readonly TaskCompletionSource<Object> m_canceledTaskTcs;

    static PredefinedTasks()
    {
      m_completedTcs = new TaskCompletionSource<object>();
      m_completedTcs.TrySetResult(null);
      m_canceledTaskTcs = new TaskCompletionSource<object>();
      m_canceledTaskTcs.TrySetCanceled();
    }

    public static Task CompletedTask
    {
      get
      {
        return m_completedTcs.Task;
      }
    }
    public static Task CanceledTaskTcs
    {
      get
      {
        return m_canceledTaskTcs.Task;
      }
    }
  }
}