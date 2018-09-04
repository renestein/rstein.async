using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class TaskExternalProxyService
  {
    private ConditionalWeakTable<Task, IProxyScheduler> m_taskSchedulerDictionary;

    public TaskExternalProxyService()
    {
      m_taskSchedulerDictionary = new ConditionalWeakTable<Task, IProxyScheduler>();
    }

    public bool AddTaskProxySchedulerPair(Task task, IProxyScheduler scheduler)
    {
      if (task == null)
      {
        throw new ArgumentNullException("task");
      }

      if (scheduler == null)
      {
        throw new ArgumentNullException("scheduler");
      }
      bool schedulerAssociatedNow = false;
      m_taskSchedulerDictionary.GetValue(task, _ =>
                                               {
                                                 schedulerAssociatedNow = true;
                                                 return scheduler;
                                               });

      return schedulerAssociatedNow;
    }

    public IProxyScheduler GetProxySchedulerForTask(Task task)
    {
      if (task == null)
      {
        throw new ArgumentNullException("task");
      }

      IProxyScheduler scheduler;
      bool result = m_taskSchedulerDictionary.TryGetValue(task, out scheduler);
      return scheduler;
    }

    public bool RemoveProxySchedulerForTask(Task task)
    {
      if (task == null)
      {
        throw new ArgumentNullException("task");
      }

      bool removedNow = m_taskSchedulerDictionary.Remove(task);
      Debug.Assert(removedNow);
      return removedNow;
    }
  }
}