using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class TaskExternalProxyService
  {
    private ConditionalWeakTable<Task, IExternalProxyScheduler> m_taskSchedulerDictionary;

    public TaskExternalProxyService()
    {
      m_taskSchedulerDictionary = new ConditionalWeakTable<Task, IExternalProxyScheduler>();
    }

    public void AddTaskSchedulerPair(Task task, IExternalProxyScheduler scheduler)
    {
      if (task == null)
      {
        throw new ArgumentNullException("task");
      }

      if (scheduler == null)
      {
        throw new ArgumentNullException("scheduler");
      }

      m_taskSchedulerDictionary.Add(task, scheduler);
    }

    public IExternalProxyScheduler GetSchedulerForTask(Task task)
    {
      if (task == null)
      {
        throw new ArgumentNullException("task");
      }
      IExternalProxyScheduler scheduler;
      bool result = m_taskSchedulerDictionary.TryGetValue(task, out scheduler);
      Debug.Assert(result);
      return scheduler;
    }
  }
}