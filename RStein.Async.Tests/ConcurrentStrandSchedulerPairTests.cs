using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Itenso.TimePeriod;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Misc;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{

  [TestClass]
  public class ConcurrentStrandSchedulerPairTests
  {
    private const int MAX_TASKS_CONCURRENCY = 4;
    private const int DEFAULT_THREAD_SLEEP = 20;
    private const int BEGIN_TASK_THREAD_SLEEP = 1;

    private ConcurrentStrandSchedulerPair m_concurrentStrandSchedulerPair;
    private TaskFactory m_strandTaskFactory;
    private TaskFactory m_concurrentTaskFactory;

    [TestInitialize]
    public void ConcurrentStrandSchedulerPairTestsTestInitialize()
    {
      m_concurrentStrandSchedulerPair = new ConcurrentStrandSchedulerPair(MAX_TASKS_CONCURRENCY);
      m_strandTaskFactory = new TaskFactory(m_concurrentStrandSchedulerPair.StrandScheduler);
      m_concurrentTaskFactory = new TaskFactory(m_concurrentStrandSchedulerPair.ConcurrentScheduler);
    }

    [TestCleanup]
    public void ConcurrentStrandSchedulerPairTestsTestCleanup()
    {
      m_concurrentStrandSchedulerPair.Dispose();

    }

    [TestMethod]
    public async Task StrandScheduler_When_Tasks_Are_Added_Then_Execution_Time_Does_Not_Intersect()
    {
      const int NUMBER_OF_TASKS = 100;

      var tasks = Enumerable.Range(0, NUMBER_OF_TASKS)
        .Select(i => m_strandTaskFactory.StartNew(() =>
        {
          Thread.Sleep(BEGIN_TASK_THREAD_SLEEP);
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

    [TestMethod]
    public async Task StrandScheduler_When_Tasks_Are_Added_And_Concurrent_Task_Are_added_Then_Execution_Of_The_Strand_Tasks_Time_Does_Not_Intersect_With_AnotherTask()
    {
      const int NUMBER_OF_STRAND_TASKS = 100;
      const int NUMBER_OF_CONCURRENT_TASKS = 1000;

      var strandTasks = Enumerable.Range(0, NUMBER_OF_STRAND_TASKS)
        .Select(i => m_strandTaskFactory.StartNew(() => getTaskTimeRange()));


      var concurrentTasks = Enumerable.Range(0, NUMBER_OF_CONCURRENT_TASKS)
        .Select(i => m_concurrentTaskFactory.StartNew(() => getTaskTimeRange()));

      var allTasks = concurrentTasks.Union(strandTasks).ToArray();

      await Task.WhenAll(allTasks);

      var timeRanges = allTasks.Select(task => task.Result);
      var timePeriodCollection = new TimePeriodCollection(timeRanges);
      var timePeriodIntersector = new TimePeriodIntersector<TimeRange>();
      var intersectPeriods = timePeriodIntersector.IntersectPeriods(timePeriodCollection);
      Assert.IsTrue(!intersectPeriods.Any());

    }

    private TimeRange getTaskTimeRange()
    {
      Thread.Sleep(BEGIN_TASK_THREAD_SLEEP);
      var startTime = DateTime.Now;
      var duration = StopWatchUtils.MeasureActionTime(() => Thread.Sleep(DEFAULT_THREAD_SLEEP));
      return new TimeRange(startTime, duration, isReadOnly: true);
    }


    [TestClass]
    public class ConcurrentStrandSchedulerPairTests_StrandSchedulerTests : IAutonomousSchedulerTests
    {

      private ConcurrentStrandSchedulerPair m_concurrentStrandSchedulerPair;

      public override void InitializeTest()
      {
        m_concurrentStrandSchedulerPair = new ConcurrentStrandSchedulerPair(MAX_TASKS_CONCURRENCY);
        base.InitializeTest();
      }

      public override void CleanupTest()
      {
        m_concurrentStrandSchedulerPair.Dispose();
        base.CleanupTest();
      }

      protected override ITaskScheduler Scheduler
      {
        get
        {
          return m_concurrentStrandSchedulerPair.AsioStrandcheduler;
        }
      }
      protected override IExternalProxyScheduler ProxyScheduler
      {
        get
        {
          return m_concurrentStrandSchedulerPair.StrandProxyScheduler;
        }
      }
    }

    [TestClass]
    public class ConcurrentStrandSchedulerPairTests_ConcurrentSchedulerTests : IAutonomousSchedulerTests
    {

      private ConcurrentStrandSchedulerPair m_concurrentStrandSchedulerPair;

      public override void InitializeTest()
      {
        m_concurrentStrandSchedulerPair = new ConcurrentStrandSchedulerPair(MAX_TASKS_CONCURRENCY);
        base.InitializeTest();
      }

      public override void CleanupTest()
      {
        m_concurrentStrandSchedulerPair.Dispose();
        base.CleanupTest();
      }

      protected override ITaskScheduler Scheduler
      {
        get
        {
          return m_concurrentStrandSchedulerPair.AsioConcurrentScheduler;
        }
      }
      protected override IExternalProxyScheduler ProxyScheduler
      {
        get
        {
          return m_concurrentStrandSchedulerPair.ConcurrentProxyScheduler;
        }
      }
    }
  }
}