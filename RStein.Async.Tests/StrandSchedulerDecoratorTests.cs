using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Itenso.TimePeriod;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Misc;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{
  [TestClass]
  public class StrandSchedulerDecoratorTests : IAutonomousSchedulerTests
  {
    public const int NUMBER_OF_THREADS = 4;
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
      return new IoServiceThreadPoolScheduler(ioService, NUMBER_OF_THREADS);
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

    [TestMethod]
    public async Task WithTaskFactory_When_Tasks_Added_Then_All_Tasks_Executed_Sequentially()
    {
      const int numberOfTasks = 10;
      const int DEFAULT_THREAD_SLEEP = 200;
      var tasks = Enumerable.Range(0, numberOfTasks)
        .Select(i => CurrentTaskFactory.StartNew(() =>
                                                 {
                                                   Thread.Sleep(DEFAULT_THREAD_SLEEP);
                                                   return DateTime.Now;
                                                 }))
        .ToArray();

      await Task.WhenAll(tasks);

      var allTasksExecutedSequentially = tasks.Aggregate(
                                                         new
                                                         {
                                                           Result = true,
                                                           PreviousTask = (Task<DateTime>)null
                                                         },
        (prevResult, currentTask) =>
        {
          if (!prevResult.Result)
          {
            return prevResult;
          }

          return new
                 {
                   Result = (prevResult.PreviousTask != null
                     ? currentTask.Result > prevResult.PreviousTask.Result : prevResult.Result),
                   PreviousTask = currentTask
                 };
        },
        resultPair => resultPair.Result);

      Assert.IsTrue(allTasksExecutedSequentially);
    }

    [TestMethod]
    public async Task WithTaskFactory_When_Tasks_Added_Then_Execution_Time_Of_The_Tasks_Does_Not_Intersect()
    {
      const int numberOfTasks = 1000;
      const int DEFAULT_THREAD_SLEEP = 200;

      var tasks = Enumerable.Range(0, numberOfTasks)
        .Select(i => CurrentTaskFactory.StartNew(() =>
                                                 {
                                                   var startTime = DateTime.Now;
                                                   var duration = StopWatchUtils.MeasureActionTime(() => Thread.Sleep(DEFAULT_THREAD_SLEEP));
                                                   return new TimeRange(startTime, duration, isReadOnly: true);
                                                 }))
        .ToArray();

      await Task.WhenAll(tasks);

      var timeRanges = tasks.Select(task => task.Result);
      var timePeriodCollection = new TimePeriodCollection(timeRanges);
      var timePeriodIntersector = new TimePeriodIntersector<TimeRange>();
      var intersectPeriods = timePeriodIntersector.IntersectPeriods(timePeriodCollection);
      Assert.IsTrue(!intersectPeriods.Any());
    }
  }
}