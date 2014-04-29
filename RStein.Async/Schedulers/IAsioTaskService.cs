using System;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public interface IAsioTaskService : IDisposable
  {
    Task Dispatch(Action action);
    Task Post(Action action);
    Action Wrap(Action action);
    Func<Task> WrapAsTask(Action action);
  }
}