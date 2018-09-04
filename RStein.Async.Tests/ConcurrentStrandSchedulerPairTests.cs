using System;
using System.Diagnostics;
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
    private TaskFactory m_concurrentTaskFactory;
    private TaskFactory m_strandTaskFactory;

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
    public async Task StrandScheduler_When_Added_Strand_And_Concurrent_Task_Then_Execution_Of_The_Strand_Tasks_Time_Does_Not_Overlap_With_Other_Tasks()
    {
      const int NUMBER_OF_STRAND_TASKS = 100;
      const int NUMBER_OF_CONCURRENT_TASKS = 1000;

      var strandTasks = Enumerable.Range(0, NUMBER_OF_STRAND_TASKS)
        .Select(i => m_strandTaskFactory.StartNew(() => getTaskTimeRange()));


      var concurrentTasks = Enumerable.Range(0, NUMBER_OF_CONCURRENT_TASKS)
        .Select(i => m_concurrentTaskFactory.StartNew(() => getTaskTimeRange())).ToArray();

      strandTasks = strandTasks.ToArray();

      var allTasks = concurrentTasks.Union(strandTasks).ToArray();

      await Task.WhenAll(allTasks);

      var strandTimeRanges = strandTasks.Select(task => task.Result).ToArray();
      var concurrentTimeRanges = concurrentTasks.Select(task => task.Result).ToArray();

      var overlaps = from strandTimeRange in strandTimeRanges
        from concurrentTimeRange in concurrentTimeRanges
        select strandTimeRange.OverlapsWith(concurrentTimeRange);

      var strandTaskIntersectWithConcurrentTask = overlaps.Any(overlap => overlap);

      Debug.Assert(strandTaskIntersectWithConcurrentTask == false);
      Assert.IsFalse(strandTaskIntersectWithConcurrentTask);
    }

    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    public void ConcurrentProxyScheduler_When_ConcurrentStrandSchedulerPair_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_concurrentStrandSchedulerPair.Dispose();
      var scheduler = m_concurrentStrandSchedulerPair.ConcurrentProxyScheduler;
    }

    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    public void StrandProxyScheduler_When_ConcurrentStrandSchedulerPair_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_concurrentStrandSchedulerPair.Dispose();
      var scheduler = m_concurrentStrandSchedulerPair.StrandProxyScheduler;
    }

    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    public void AsioStrandcheduler_When_ConcurrentStrandSchedulerPair_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_concurrentStrandSchedulerPair.Dispose();
      var scheduler = m_concurrentStrandSchedulerPair.AsioStrandcheduler;
    }

    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    public void AsioConcurrentScheduler_When_ConcurrentStrandSchedulerPair_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_concurrentStrandSchedulerPair.Dispose();
      var scheduler = m_concurrentStrandSchedulerPair.AsioConcurrentScheduler;
    }

    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    public void ConcurrentScheduler_When_ConcurrentStrandSchedulerPair_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_concurrentStrandSchedulerPair.Dispose();
      var scheduler = m_concurrentStrandSchedulerPair.ConcurrentScheduler;
    }

    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    public void StrandScheduler_When_ConcurrentStrandSchedulerPair_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_concurrentStrandSchedulerPair.Dispose();
      var scheduler = m_concurrentStrandSchedulerPair.StrandScheduler;
    }

    [TestMethod]
    public void Dispose_Repeated_Call_Does_Not_Throw()
    {
      m_concurrentStrandSchedulerPair.Dispose();
      m_concurrentStrandSchedulerPair.Dispose();
    }

    [TestMethod]
    public void Dispose_Does_Not_Throw()
    {
      m_concurrentStrandSchedulerPair.Dispose();
    }

    private TimeRange getTaskTimeRange()
    {
      Thread.Sleep(BEGIN_TASK_THREAD_SLEEP);
      var startTime = DateTime.Now;
      var duration = StopWatchUtils.MeasureActionTime(() => Thread.Sleep(DEFAULT_THREAD_SLEEP));
      return new TimeRange(startTime, duration, isReadOnly: true);
    }


    [TestClass]
    public class ConcurrentStrandSchedulerPairTests_ConcurrentSchedulerTests : IAutonomousSchedulerTests
    {
      private ConcurrentStrandSchedulerPair m_concurrentStrandSchedulerPair;
      private ITaskScheduler m_strandScheduler;

      protected override ITaskScheduler Scheduler
      {
        get
        {
          return m_strandScheduler;
        }
      }
      protected override IProxyScheduler ProxyScheduler
      {
        get
        {
          return m_strandScheduler.ProxyScheduler;
        }
      }

      public override void InitializeTest()
      {
        m_concurrentStrandSchedulerPair = new ConcurrentStrandSchedulerPair(MAX_TASKS_CONCURRENCY);
        m_strandScheduler = m_concurrentStrandSchedulerPair.AsioConcurrentScheduler;
        base.InitializeTest();
      }

      public override void CleanupTest()
      {
        m_concurrentStrandSchedulerPair.Dispose();
        base.CleanupTest();
      }
    }

    [TestClass]
    public class ConcurrentStrandSchedulerPairTests_StrandSchedulerTests : IAutonomousSchedulerTests
    {
      private ITaskScheduler m_concurrentScheduler;
      private ConcurrentStrandSchedulerPair m_concurrentStrandSchedulerPair;

      protected override ITaskScheduler Scheduler
      {
        get
        {
          return m_concurrentScheduler;
        }
      }
      protected override IProxyScheduler ProxyScheduler
      {
        get
        {
          return m_concurrentScheduler.ProxyScheduler;
        }
      }

      public override void InitializeTest()
      {
        m_concurrentStrandSchedulerPair = new ConcurrentStrandSchedulerPair(MAX_TASKS_CONCURRENCY);
        m_concurrentScheduler = m_concurrentStrandSchedulerPair.AsioStrandcheduler;
        base.InitializeTest();
      }

      public override void CleanupTest()
      {
        m_concurrentStrandSchedulerPair.Dispose();
        base.CleanupTest();
      }
    }
  }
}