using System;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public interface IAsioTaskService : IDisposable
  {
    Task Dispatch(Action action);
    Task Dispatch(Func<Task> function);

    Task Post(Action action);
    Task Post(Func<Task> function);

    Action Wrap(Action action);
    Action Wrap(Func<Task> function);
  }
}