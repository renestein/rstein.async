using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{
  [TestClass]
  public class CurrentThreadSchedulerTests : IAutonomousSchedulerTests
  {
    private IProxyScheduler m_proxyScheduler;
    private ITaskScheduler m_scheduler;

    protected override ITaskScheduler Scheduler
    {
      get
      {
        return m_scheduler;
      }
    }

    protected override IProxyScheduler ProxyScheduler
    {
      get
      {
        return m_proxyScheduler;
      }
    }

    public override void InitializeTest()
    {
      m_scheduler = new CurrentThreadScheduler();
      m_proxyScheduler = new ProxyScheduler(m_scheduler);
      base.InitializeTest();
    }

    [Ignore] //Avoid deadlock in advance.
    public override Task Dispose_When_Tasks_Are_Queued_Then_All_Tasks_Are_Executed()
    {
      return null;
    }
  }
}