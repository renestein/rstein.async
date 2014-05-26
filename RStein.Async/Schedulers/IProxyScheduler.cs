using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public interface IProxyScheduler
  {
    bool DoTryExecuteTask(Task task);
    TaskScheduler AsTplScheduler();
  }
}