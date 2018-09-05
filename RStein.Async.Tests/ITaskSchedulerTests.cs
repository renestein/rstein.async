using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{
  [TestClass]
  public abstract class ITaskSchedulerTests
  {
    protected abstract ITaskScheduler Scheduler
    {
      get;
    }

    [TestInitialize]
    public void ITaskSchedulerTestsInitialize()
    {
      InitializeTest();
    }


    [TestCleanup]
    public void ITaskSchedulerTestsCleanup()
    {
      CleanupTest();
    }

    public virtual void CleanupTest()
    {
      Scheduler.Dispose();
    }

    public virtual void InitializeTest() {}

    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    public void QueueTask_When_TaskScheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
   
      var dummyTask = new Task(() => {});

      Scheduler.Dispose();
      Scheduler.QueueTask(dummyTask);
    }


    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    public void TryExecuteTaskInline_When_TaskScheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      var dummyTask = new Task(() => {});

      Scheduler.Dispose();

      Scheduler.TryExecuteTaskInline(dummyTask, false);
    }

    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    public void GetScheduledTasks_When_TaskScheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      Scheduler.Dispose();
      Scheduler.GetScheduledTasks();
    }

    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    public void MaximumConcurrencyLevel_When_TaskScheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      Scheduler.Dispose();
      var maximumConcurrencyLevel = Scheduler.MaximumConcurrencyLevel;
    }

    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    private void SetProxyScheduler__When_TaskScheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      Scheduler.Dispose();
      Scheduler.ProxyScheduler = null;
    }

    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    private void GetProxyScheduler__When_TaskScheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      Scheduler.Dispose();
      var proxyScheduler = Scheduler.ProxyScheduler;
    }


    [TestMethod]
    public void Dispose_Repeated_Call_Does_Not_Throw()
    {
      Scheduler.Dispose();
      Scheduler.Dispose();
    }

    [TestMethod]
    public void Dispose_Does_Not_Throw()
    {
      Scheduler.Dispose();
    }
  }
}