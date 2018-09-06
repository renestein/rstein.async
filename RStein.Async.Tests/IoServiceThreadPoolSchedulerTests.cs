using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{
  [TestClass]
  public class IoServiceThreadPoolSchedulerTests : IAutonomousSchedulerTests
  {
    private ProxyScheduler m_proxyScheduler;
    private IoServiceScheduler m_ioService;
    private ITaskScheduler m_threadPool;

    protected override IProxyScheduler ProxyScheduler => m_proxyScheduler;

    protected override ITaskScheduler Scheduler => m_threadPool;


    public override void InitializeTest()
    {
      m_ioService = new IoServiceScheduler();
      m_threadPool = new IoServiceThreadPoolScheduler(m_ioService);
      m_proxyScheduler = new ProxyScheduler(m_threadPool);
      base.InitializeTest();
    }


    public override void CleanupTest()
    {
      m_threadPool.Dispose();
      base.CleanupTest();
    }

    [TestMethod]
    [ExpectedException(typeof (ArgumentNullException))]
    public void Ctor_When_Io_Service_Is_Null_Then_Throws_ArgumentException()
    {
      var threadPool = new IoServiceThreadPoolScheduler(null);
    }

    [TestMethod]
    [ExpectedException(typeof (ArgumentOutOfRangeException))]
    public void Ctor_When_Number_Of_Threads_Is_Zero_Then_Throws_ArgumentOutOfRangeException()
    {
      var threadPool = new IoServiceThreadPoolScheduler(m_ioService, 0);
    }

    [TestMethod]
    [ExpectedException(typeof (ArgumentOutOfRangeException))]
    public void Ctor_When_Number_Of_Threads_Is_Negative_Then_Throws_ArgumentOutOfRangeException()
    {
      var threadPool = new IoServiceThreadPoolScheduler(m_ioService, -1);
    }
  }
}