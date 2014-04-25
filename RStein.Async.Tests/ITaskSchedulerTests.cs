using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{
  public abstract class ITaskSchedulerTests
  {

    protected abstract ITaskScheduler Scheduler
    {
      get;
    }


    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void QueueTask_When_TaskScheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      var dummyTask = new Task(() =>
                              {
                              });

      Scheduler.Dispose();
      Scheduler.QueueTask(dummyTask);
    }


    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void TryExecuteTaskInline_When_TaskScheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      var dummyTask = new Task(() =>
                              {
                              });

      Scheduler.Dispose();

      Scheduler.QueueTask(dummyTask);
    }

    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void GetScheduledTasks_When_TaskScheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      Scheduler.Dispose();
      Scheduler.GetScheduledTasks();
    }

    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void MaximumConcurrencyLevel_When_TaskScheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      Scheduler.Dispose();
      var maximumConcurrencyLevel = Scheduler.MaximumConcurrencyLevel;
    }
    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    void SetProxyScheduler__When_TaskScheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      Scheduler.Dispose();
      Scheduler.SetProxyScheduler(null);
    }

    [TestMethod]
    public void Dispose_Repeated_Call_Does_Not_Throw()
    {
      Scheduler.Dispose();
      Scheduler.Dispose();
    }
  }
}