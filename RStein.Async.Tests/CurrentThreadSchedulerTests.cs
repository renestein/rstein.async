using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{
  [TestClass]
  public class CurrentThreadSchedulerTests : IAutonomousSchedulerTests
  {
    private ITaskScheduler m_scheduler;
    private IExternalProxyScheduler m_proxyScheduler;

    protected override ITaskScheduler Scheduler
    {
      get
      {
        return m_scheduler;
      }
    }

    protected override IExternalProxyScheduler ProxyScheduler
    {
      get
      {
        return m_proxyScheduler;
      }
    }

    public override void InitializeTest()
    {
      m_scheduler = new CurrentThreadScheduler();
      m_proxyScheduler = new ExternalProxyScheduler(m_scheduler);
      base.InitializeTest();
    }

    [Ignore] //Prevent deadlock - CurrentThreadSchedular is special scheduler
    public override Task Dispose_When_Tasks_Are_Queued_Then_All_Tasks_Are_Executed()
    {
      return null;
    }
  }
}