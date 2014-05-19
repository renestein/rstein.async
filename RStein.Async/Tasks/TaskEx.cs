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

    public static Task PrepareTcsTaskFromExistingTask<T>(this Task<T> originaltask, TaskCompletionSource<T> taskcompletionSource)
    {
      checkPrepareTcsTaskArgs(originaltask, taskcompletionSource);
      addTaskFromExistingTaskProblemContinuation(originaltask, taskcompletionSource);
      addRunToCompletionContinuation(originaltask, taskcompletionSource);
      return taskcompletionSource.Task;
    }

    public static Task PrepareTcsTaskFromExistingTask(this Task originaltask, TaskCompletionSource<Object> taskcompletionSource)
    {
      checkPrepareTcsTaskArgs(originaltask, taskcompletionSource);

      addTaskFromExistingTaskProblemContinuation(originaltask, taskcompletionSource);
      originaltask.ContinueWith(_ => taskcompletionSource.TrySetResult(null),
                                CancellationToken.None,
                                TaskContinuationOptions.OnlyOnRanToCompletion,
                                TaskScheduler.Default);

      return taskcompletionSource.Task;
    }

    private static void checkPrepareTcsTaskArgs(Task originaltask, object taskcompletionSource)
    {
      if (originaltask == null)
      {
        throw new ArgumentNullException("originaltask");
      }

      if (taskcompletionSource == null)
      {
        throw new ArgumentNullException("taskcompletionSource");
      }
    }

    public static Task<T> CreateTaskFromExistingTask<T>(this Task<T> originalTask)
    {
      if (originalTask == null)
      {
        throw new ArgumentNullException("originalTask");
      }

      var tcs = addTaskFromExistingTaskProblemContinuation<T>(originalTask);
      addRunToCompletionContinuation(originalTask, tcs);

      return tcs.Task;

    }

    public static void WaitAndPropagateException(this Task task)
    {
      if (task == null)
      {
        throw new ArgumentNullException("task");
      }

      task.GetAwaiter().GetResult();
    }

    private static void addRunToCompletionContinuation<T>(Task<T> originalTask, TaskCompletionSource<T> tcs)
    {
      originalTask.ContinueWith(_ => tcs.TrySetResult(originalTask.Result),
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnRanToCompletion,
        TaskScheduler.Default);
    }

    public static Task CreateTaskFromExistingTask(Task originalTask)
    {
      if (originalTask == null)
      {
        throw new ArgumentNullException("originalTask");
      }

      var tcs = addTaskFromExistingTaskProblemContinuation<Object>(originalTask);
      return tcs.Task;

    }

    private static TaskCompletionSource<T> addTaskFromExistingTaskProblemContinuation<T>(Task originalTask, TaskCompletionSource<T> proxyTcs = null)
    {

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