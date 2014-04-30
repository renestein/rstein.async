using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{
  [TestClass]
  public class StrandSchedulerDecoratorWithOneThreadScheduler : StrandSchedulerDecoratorTests
  {
    public const int NUMBER_OF_THREADS_IN_IMPLICIT_STRAND = 1;

    protected override ITaskScheduler CreateInnerScheduler()
    {
      var ioService = new IoServiceScheduler();
      return new IoServiceThreadPoolScheduler(ioService, NUMBER_OF_THREADS_IN_IMPLICIT_STRAND);
    }
  }
}