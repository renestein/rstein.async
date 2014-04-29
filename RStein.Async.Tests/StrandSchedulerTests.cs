using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{
  [TestClass]
  public class StrandSchedulerTests : IAutonomousSchedulerTests
  {
    public const int NUMBER_OF_THREAD = 4;
    private ITaskScheduler m_strandScheduler;    
    private ExternalProxyScheduler m_externalScheduler;

    public override void InitializeTest()
    {
      m_strandScheduler = new StrandSchedulerDecorator(GetInnerScheduler());
      m_externalScheduler = new ExternalProxyScheduler(m_strandScheduler);
      base.InitializeTest();
    }

    public override void CleanupTest()
    {
      m_strandScheduler.Dispose();
      GetInnerScheduler().Dispose();
      base.CleanupTest();
    }

    protected virtual ITaskScheduler GetInnerScheduler()
    {
      var ioService = new IoServiceScheduler();
      return new IoServiceThreadPoolScheduler(ioService, NUMBER_OF_THREAD);
    }

    protected override ITaskScheduler Scheduler
    {
      get
      {
        return m_strandScheduler;
      }
    }

    protected override IExternalProxyScheduler ProxyScheduler
    {
      get
      {
        return m_externalScheduler;
      }
    }
  }
}
