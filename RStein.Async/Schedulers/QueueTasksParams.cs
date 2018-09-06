using System;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class QueueTasksParams
  {
    public QueueTasksParams(int maxNumberOfQueuedTasks = Int32.MaxValue,
      Action<Task> beforeTaskQueuedAction = null,
      Action<Task> taskContinuation = null,
      Action<Task> afterTaskQueuedAction = null)
    {
      MaxNumberOfQueuedTasks = maxNumberOfQueuedTasks;
      BeforeTaskQueuedAction = beforeTaskQueuedAction;
      TaskContinuation = taskContinuation;
      AfterTaskQueuedAction = afterTaskQueuedAction;
    }

    public Action<Task> BeforeTaskQueuedAction
    {
      get;
    }

    public Action<Task> TaskContinuation
    {
      get;
    }

    public Action<Task> AfterTaskQueuedAction
    {
      get;
    }

    public int MaxNumberOfQueuedTasks
    {
      get;
    }
  }
}