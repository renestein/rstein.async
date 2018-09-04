using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{
  [TestClass]
  public class StrandSchedulerDecoratorWithIoServiceThreadPoolTests : StrandSchedulerDecoratorTests
  {
    public const int NUMBER_OF_THREADS = 4;

    protected override ITaskScheduler CreateInnerScheduler()
    {
      var ioService = new IoServiceScheduler();
      return new IoServiceThreadPoolScheduler(ioService, NUMBER_OF_THREADS);
    }
  }
}