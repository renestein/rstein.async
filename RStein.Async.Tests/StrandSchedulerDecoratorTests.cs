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
  public class StrandSchedulerDecoratorTests : IAutonomousSchedulerTests
  {
    public const int NUMBER_OF_THREADS = 4;
    private const int INVALID_TASK_ID = 0;
    private const int INVALID_THREAD_ID = -1;
    private StrandSchedulerDecorator m_strandScheduler;
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
      m_externalScheduler.Dispose();
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
      const int NUMBER_OF_TASKS = 100;
      const int DEFAULT_THREAD_SLEEP = 20;
      const int BEGIN_TASK_THREAD_SLEEP = 1;

      var tasks = Enumerable.Range(0, NUMBER_OF_TASKS)
        .Select(i => CurrentTaskFactory.StartNew(() =>
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
    public async Task WithTaskFactory_When_Tasks_Added_And_Acquiring_DummyLock_Then_All_Tasks_Executed_Sequentially()
    {
      const int numberOfTasks = 100;
      const int DEFAULT_THREAD_SLEEP = 2;
      var lockRoot = new object();

      var tasks = Enumerable.Range(0, numberOfTasks)
        .Select(i => CurrentTaskFactory.StartNew(() =>
                                                 {
                                                   bool lockTaken = false;
                                                   Monitor.TryEnter(lockRoot, ref lockTaken);
                                                   try
                                                   {
                                                     Thread.Sleep(DEFAULT_THREAD_SLEEP);
                                                   }
                                                   finally
                                                   {
                                                     if (lockTaken)
                                                     {
                                                       Monitor.Exit(lockRoot);
                                                     }

                                                   }
                                                   return lockTaken;

                                                 }))
        .ToArray();

      await Task.WhenAll(tasks);
      bool executedSequentially = tasks.All(task => task.Result);
      Assert.IsTrue(executedSequentially);
    }

    [TestMethod]
    public async Task Dispatch_When_Called_Inside_Strand_Action_Is_Executed_Inline()
    {
      var originalTaskThreadId = INVALID_THREAD_ID;
      var inDispatchTaskThreadId = INVALID_THREAD_ID;

      var task = CurrentTaskFactory.StartNew(() =>
                                  {
                                    originalTaskThreadId = Thread.CurrentThread.ManagedThreadId;
                                    m_strandScheduler.Dispatch(() => inDispatchTaskThreadId = Thread.CurrentThread.ManagedThreadId).Wait();
                                  });

      await task;
      Assert.AreNotEqual(INVALID_THREAD_ID, originalTaskThreadId);
      Assert.AreNotEqual(INVALID_THREAD_ID, inDispatchTaskThreadId);
      Assert.AreEqual(originalTaskThreadId, originalTaskThreadId);
    }

    [TestMethod]
    public async Task Dispatch_When_Called_Outside_Strand_Action_Is_Posted()
    {
      var originalTaskId = INVALID_TASK_ID;
      var inDispatchTaskId = INVALID_TASK_ID;
      Task innerTask = null;
      var outerTask = Task.Run(() =>
      {
        originalTaskId = Task.CurrentId.Value;
        innerTask = m_strandScheduler.Dispatch(() => inDispatchTaskId = Task.CurrentId.Value);
      });

      await outerTask;
      await innerTask;

      Assert.AreNotEqual(INVALID_TASK_ID, originalTaskId);
      Assert.AreNotEqual(INVALID_TASK_ID, inDispatchTaskId);
      Assert.AreNotEqual(originalTaskId, inDispatchTaskId);
    }


    [TestMethod]
    public async Task Post_When_Called_Outside_Strand_Action_Is_Posted()
    {
      var originalTaskId = INVALID_TASK_ID;
      var inDispatchTaskId = INVALID_TASK_ID;

      Task innerTask = null;
      var outerTask = Task.Run(() =>
      {
        originalTaskId = Task.CurrentId.Value;
        innerTask = m_strandScheduler.Post(() => inDispatchTaskId = Task.CurrentId.Value);
      });

      await outerTask;
      await innerTask;

      Assert.AreNotEqual(INVALID_TASK_ID, originalTaskId);
      Assert.AreNotEqual(INVALID_TASK_ID, inDispatchTaskId);
      Assert.AreNotEqual(originalTaskId, inDispatchTaskId);
    }

    [TestMethod]
    public async Task Post_When_Called_Inside_Strand_Action_Is_Posted()
    {
      var originalTaskId = INVALID_TASK_ID;
      var inDispatchTaskId = INVALID_TASK_ID;

      Task innerTask = null;
      var outerTask = CurrentTaskFactory.StartNew(() =>
      {
        originalTaskId = Task.CurrentId.Value;
        innerTask = m_strandScheduler.Post(() => inDispatchTaskId = Task.CurrentId.Value);
      });

      await outerTask;
      await innerTask;

      Assert.AreNotEqual(INVALID_TASK_ID, originalTaskId);
      Assert.AreNotEqual(INVALID_TASK_ID, inDispatchTaskId);
      Assert.AreNotEqual(originalTaskId, inDispatchTaskId);
    }

    [TestMethod]
    public void Wrap_When_Wrapped_Action_Is_Not_Manually_Invoked_Then_Original_Action_Does_Not_Execute()
    {
      bool wasActionExecuted = false;
      var wrappedAction = m_strandScheduler.Wrap(() => wasActionExecuted = true);
      m_strandScheduler.Dispose();
      Assert.IsFalse(wasActionExecuted);
    }

    [TestMethod]
    public void Wrap_When_Wrapped_Action_Is_Invoked_Then_Original_Action_Executed()
    {
      bool wasActionExecuted = false;
      var wrappedAction = m_strandScheduler.Wrap(() => wasActionExecuted = true);
      wrappedAction();
      m_strandScheduler.Dispose();
      Assert.IsTrue(wasActionExecuted);
    }
  }
}