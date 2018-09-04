using System;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public static class ProxySchedulerTaskExtensions
  {
    private static readonly TaskExternalProxyService _taskSchedulerExternalProxyService = new TaskExternalProxyService();

    public static bool SetProxyScheduler(this Task task, IProxyScheduler proxyScheduler)
    {
      if (task == null)
      {
        throw new ArgumentNullException("task");
      }

      if (proxyScheduler == null)
      {
        throw new ArgumentNullException("proxyScheduler");
      }
      return _taskSchedulerExternalProxyService.AddTaskProxySchedulerPair(task, proxyScheduler);
    }

    public static IProxyScheduler GetProxyScheduler(this Task task)
    {
      if (task == null)
      {
        throw new ArgumentNullException("task");
      }

      return _taskSchedulerExternalProxyService.GetProxySchedulerForTask(task);
    }

    public static bool RunOnProxyScheduler(this Task task)
    {
      if (task == null)
      {
        throw new ArgumentNullException("task");
      }

      var externalProxyScheduler = GetProxyScheduler(task);

      if (externalProxyScheduler == null)
      {
        return false;
      }

      return externalProxyScheduler.DoTryExecuteTask(task);
    }

    public static bool RemoveProxyScheduler(this Task task)
    {
      if (task == null)
      {
        throw new ArgumentNullException("task");
      }

      return _taskSchedulerExternalProxyService.RemoveProxySchedulerForTask(task);
    }
  }
}