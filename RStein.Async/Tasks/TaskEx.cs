using System;
using System.Threading.Tasks;

namespace RStein.Async.Tasks
{
  public static class TaskEx
  {


    public static Task TaskFromException(Exception exception)
    {
      var tcs = new TaskCompletionSource<Object>();
      tcs.SetException(exception);
      return tcs.Task;
    }

    public static Task TaskFromSynchronnousAction(Action action)
    {
      if (action == null)
      {
        throw new ArgumentNullException("action");
      }

      try
      {
        action();
        return PredefinedTasks.CompletedTask;
      }
      catch (Exception exception)
      {
        return TaskFromException(exception);
      }
    }
  }
}