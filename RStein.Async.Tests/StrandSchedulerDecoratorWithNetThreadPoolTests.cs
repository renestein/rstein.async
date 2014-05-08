using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{
  [TestClass]
  public class StrandSchedulerDecoratorWithNetThreadPoolTests : StrandSchedulerDecoratorTests
  {    
    protected override ITaskScheduler CreateInnerScheduler()
    {
      return new NetThreadPoolSchedulerAdapter();
    }
  }
}