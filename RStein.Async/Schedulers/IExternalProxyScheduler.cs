using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public interface IExternalProxyScheduler
  {    
    bool DoTryExecuteTask(Task task);
    TaskScheduler AsRealScheduler();
  }
}