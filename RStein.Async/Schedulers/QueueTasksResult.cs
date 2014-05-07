using System;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class QueueTasksResult
  {
    private readonly int m_numberOfQueuedTasks;
    private readonly Task m_whenAllTask;
    private readonly bool m_hasMoreTasks;

    public QueueTasksResult(int numberOfQueuedTasks, Task whenAllTask,  bool hasMoreTasks)
    {
      if (numberOfQueuedTasks < 0)
      {
        throw new ArgumentException("numberOfQueuedTasks");
      }
      m_numberOfQueuedTasks = numberOfQueuedTasks;
      m_whenAllTask = whenAllTask;
      m_hasMoreTasks = hasMoreTasks;
    }
  }
}