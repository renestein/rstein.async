using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{
  [TestClass]
  public class ConcurrentStrandSchedulerPair_Strand_Tests : IAutonomousSchedulerTests
  {
    protected override ITaskScheduler Scheduler
    {
      get
      {
        throw new System.NotImplementedException();
      }
    }
    protected override IExternalProxyScheduler ProxyScheduler
    {
      get
      {
        throw new System.NotImplementedException();
      }
    }
  }
}