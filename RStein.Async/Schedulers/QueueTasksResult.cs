using System;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class QueueTasksResult
  {
    private readonly int m_numberOfQueuedTasks;
    private readonly Task m_whenAllTask;
    private readonly bool m_hasMoreTasks;

    public QueueTasksResult(int numberOfQueuedTasks, Task whenAllTask, bool hasMoreTasks)
    {
      if (numberOfQueuedTasks < 0)
      {
        throw new ArgumentException("numberOfQueuedTasks");
      }

      if (whenAllTask == null)
      {
        throw new ArgumentNullException("whenAllTask");
      }
      m_numberOfQueuedTasks = numberOfQueuedTasks;
      m_whenAllTask = whenAllTask;
      m_hasMoreTasks = hasMoreTasks;
    }

    public int NumberOfQueuedTasks
    {
      get
      {
        return m_numberOfQueuedTasks;
      }
    }

    public bool HasMoreTasks
    {
      get
      {
        return m_hasMoreTasks;
      }
    }
    public Task WhenAllTask
    {
      get
      {
        return m_whenAllTask;
      }
    }
  }
}