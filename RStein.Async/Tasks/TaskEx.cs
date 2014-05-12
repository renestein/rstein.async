using System;
using System.Threading;
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

    public static Task PrepareTcsTaskFromExistingTask<T>(this Task originaltask, TaskCompletionSource<T> taskcompletionSource)
    {
      if (originaltask == null)
      {
        throw new ArgumentNullException("originaltask");
      }

      if (taskcompletionSource == null)
      {
        throw new ArgumentNullException("taskcompletionSource");
      }

      addTaskFromExistingTaskContinuation(originaltask, taskcompletionSource);
      return taskcompletionSource.Task;
    }

    public static Task<T> CreateTaskFromExistingTask<T>(this Task<T> originalTask)
    {
      var tcs = addTaskFromExistingTaskContinuation<T>(originalTask);
      originalTask.ContinueWith(_ => tcs.TrySetResult(originalTask.Result),
                                CancellationToken.None,
                                TaskContinuationOptions.OnlyOnRanToCompletion,
                                TaskScheduler.Default);

      return tcs.Task;

    }

    public static Task CreateTaskFromExistingTask(Task originalTask)
    {

      var tcs = addTaskFromExistingTaskContinuation<Object>(originalTask);
      return tcs.Task;

    }

    private static TaskCompletionSource<T> addTaskFromExistingTaskContinuation<T>(Task originalTask, TaskCompletionSource<T> proxyTcs = null)
    {
      if (originalTask == null)
      {
        throw new ArgumentNullException("originalTask");
      }

      var proxyTaskCompletionSource = proxyTcs ?? new TaskCompletionSource<T>();
      originalTask.ContinueWith(_ =>
             {
               if (originalTask.IsCanceled)
               {
                 proxyTaskCompletionSource.TrySetCanceled();
               }
               else if (originalTask.IsFaulted)
               {
                 proxyTaskCompletionSource.TrySetException(originalTask.Exception);
               }
             }, TaskScheduler.Default);

      return proxyTaskCompletionSource;
    }
  }
}