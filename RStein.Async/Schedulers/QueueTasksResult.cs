using System;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class QueueTasksResult
  {
    public QueueTasksResult(int numberOfQueuedTasks, Task whenAllTask, bool hasMoreTasks)
    {
      if (numberOfQueuedTasks < 0)
      {
        throw new ArgumentException("numberOfQueuedTasks");
      }

      NumberOfQueuedTasks = numberOfQueuedTasks;
      WhenAllTask = whenAllTask ?? throw new ArgumentNullException(nameof(whenAllTask));
      HasMoreTasks = hasMoreTasks;
    }

    public int NumberOfQueuedTasks
    {
      get;
    }

    public bool HasMoreTasks
    {
      get;
    }

    public Task WhenAllTask
    {
      get;
    }
  }
}