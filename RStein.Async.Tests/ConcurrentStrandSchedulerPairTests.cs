using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{

  [TestClass]
  public class ConcurrentStrandSchedulerPairTests
  {
    private const int MAX_TASKS_CONCURRENCY = 4;

    [TestClass]
    public class ConcurrentStrandSchedulerPairTests_StrandSchedulerTests : IAutonomousSchedulerTests
    {

      private ConcurrentStrandSchedulerPair m_concurrentStrandSchedulerPair;

      public override void InitializeTest()
      {
        m_concurrentStrandSchedulerPair = new ConcurrentStrandSchedulerPair(MAX_TASKS_CONCURRENCY);
        base.InitializeTest();
      }

      public override void CleanupTest()
      {
        m_concurrentStrandSchedulerPair.Dispose();
        base.CleanupTest();
      }

      protected override ITaskScheduler Scheduler
      {
        get
        {
          return m_concurrentStrandSchedulerPair.AsioStrandcheduler;
        }
      }
      protected override IExternalProxyScheduler ProxyScheduler
      {
        get
        {
          return m_concurrentStrandSchedulerPair.StrandProxyScheduler;
        }
      }
    }

    [TestClass]
    public class ConcurrentStrandSchedulerPairTests_ConcurrentSchedulerTests : IAutonomousSchedulerTests
    {

      private ConcurrentStrandSchedulerPair m_concurrentStrandSchedulerPair;

      public override void InitializeTest()
      {
        m_concurrentStrandSchedulerPair = new ConcurrentStrandSchedulerPair(MAX_TASKS_CONCURRENCY);
        base.InitializeTest();
      }

      public override void CleanupTest()
      {
        m_concurrentStrandSchedulerPair.Dispose();
        base.CleanupTest();
      }

      protected override ITaskScheduler Scheduler
      {
        get
        {
          return m_concurrentStrandSchedulerPair.AsioConcurrentScheduler;
        }
      }
      protected override IExternalProxyScheduler ProxyScheduler
      {
        get
        {
          return m_concurrentStrandSchedulerPair.ConcurrentProxyScheduler;
        }
      }
    }
  }
}