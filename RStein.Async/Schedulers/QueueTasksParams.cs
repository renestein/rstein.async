using System;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class QueueTasksParams
  {
    private readonly int m_maxNumberOfQueuedtasks;
    private Action<Task> m_beforeTaskQueuedAction;
    private Action<Task> m_taskContinuation;
    private Action<Task> m_afterTaskQueuedAction;

    public QueueTasksParams(int maxNumberOfQueuedtasks = Int32.MaxValue,
      Action<Task> beforeTaskQueuedAction = null,
      Action<Task> taskContinuation = null,
      Action<Task> afterTaskQueuedAction = null)
    {
      m_maxNumberOfQueuedtasks = maxNumberOfQueuedtasks;
      m_beforeTaskQueuedAction = beforeTaskQueuedAction;
      m_taskContinuation = taskContinuation;
      m_afterTaskQueuedAction = afterTaskQueuedAction;
    }

    public Action<Task> BeforeTaskQueuedAction
    {
      get
      {
        return m_beforeTaskQueuedAction;
      }
    }
    public Action<Task> TaskContinuation
    {
      get
      {
        return m_taskContinuation;
      }
    }
    public Action<Task> AfterTaskQueuedAction
    {
      get
      {
        return m_afterTaskQueuedAction;
      }
    }
    public int MaxNumberOfQueuedtasks
    {
      get
      {
        return m_maxNumberOfQueuedtasks;
      }
    }
  }
}