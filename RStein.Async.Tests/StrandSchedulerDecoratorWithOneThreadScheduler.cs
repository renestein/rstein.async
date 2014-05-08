using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
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

    [TestMethod]
    public async Task WithTaskFactory_When_Tasks_Finished_Then_All_Tasks_Executed_In_Same_Thread()
    {
      const int NUMBER_OF_TASKS = 1000;

      var tasks = Enumerable.Range(0, NUMBER_OF_TASKS)
                             .Select(_ => TestTaskFactory.StartNew(() => Thread.CurrentThread.ManagedThreadId)).ToArray();

      await Task.WhenAll(tasks);
      int threadId = tasks.First().Result;
      bool allTaksInSameThread = tasks.All(task => task.Result == threadId);
      Assert.IsTrue(allTaksInSameThread);

    }
  }
}