using System;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public static class TaskkWithProxySchedulerExtensions
  {
    private static readonly TaskExternalProxyService _taskSchedulerExternalProxyService = new TaskExternalProxyService();

    public static void SetProxyScheduler(this Task task, IExternalProxyScheduler externalProxyScheduler)
    {
      if (task == null)
      {
        throw new ArgumentNullException("task");
      }

      if (externalProxyScheduler == null)
      {
        throw new ArgumentNullException("externalProxyScheduler");
      }
      _taskSchedulerExternalProxyService.AddTaskProxySchedulerPair(task, externalProxyScheduler);
    }

    public static IExternalProxyScheduler GetProxyScheduler(this Task task)
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