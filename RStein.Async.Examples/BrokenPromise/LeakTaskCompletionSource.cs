using System;
using System.Threading.Tasks;
using RStein.Async.Tasks;

namespace RStein.Async.Examples.BrokenPromise
{
  public class LeakTaskCompletionSource
  {
    public Task Leak()
    {
      var task = getTask();
      return task;
    }

    private Task getTask()
    {
      var dtcs = new DebugTaskCompletionSource<Object>();
      var task =  dtcs.Task;      
      return task;
    }
  }
}