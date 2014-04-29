using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{
  [TestClass]
  public class StrandSchedulerDecoratorTests : IAutonomousSchedulerTests
  {
    public const int NUMBER_OF_THREAD = 4;
    private ITaskScheduler m_strandScheduler;
    private ExternalProxyScheduler m_externalScheduler;
    private ITaskScheduler m_innerScheduler;

    public override void InitializeTest()
    {
      m_innerScheduler = CreateInnerScheduler();
      m_strandScheduler = new StrandSchedulerDecorator(m_innerScheduler);
      m_externalScheduler = new ExternalProxyScheduler(m_strandScheduler);      
      base.InitializeTest();
    }

    public override void CleanupTest()
    {
      m_strandScheduler.Dispose();
      m_innerScheduler.Dispose();
      base.CleanupTest();
    }

    protected virtual ITaskScheduler CreateInnerScheduler()
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
