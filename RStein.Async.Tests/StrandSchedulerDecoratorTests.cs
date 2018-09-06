using System;
using System.Collections.Concurrent;
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
  public abstract class StrandSchedulerDecoratorTests : IAutonomousSchedulerTests
  {
    private const int INVALID_TASK_ID = 0;
    private const int INVALID_THREAD_ID = -1;
    private ProxyScheduler m_proxyScheduler;
    private ITaskScheduler m_innerScheduler;
    private StrandSchedulerDecorator m_strandScheduler;
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
        return m_proxyScheduler;
      }
    }

    public override void InitializeTest()
    {
      m_innerScheduler = CreateInnerScheduler();
      m_strandScheduler = new StrandSchedulerDecorator(m_innerScheduler);
      m_proxyScheduler = new ProxyScheduler(m_strandScheduler);
      base.InitializeTest();
    }

    protected abstract ITaskScheduler CreateInnerScheduler();

    public override void CleanupTest()
    {
      m_strandScheduler.Dispose();
      m_innerScheduler.Dispose();
      m_proxyScheduler.Dispose();
      base.CleanupTest();
    }

    [TestMethod]
    public async Task WithTaskFactory_When_Tasks_Added_Then_All_Tasks_Executed_Sequentially()
    {
      const int numberOfTasks = 10;
      const int DEFAULT_THREAD_SLEEP = 200;
      var tasks = Enumerable.Range(0, numberOfTasks)
        .Select(i => TestTaskFactory.StartNew(() =>
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
                                                           PreviousTask = (Task<DateTime>) null
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
        .Select(i => TestTaskFactory.StartNew(() =>
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
        .Select(i => TestTaskFactory.StartNew(() =>
                                              {
                                                var lockTaken = false;
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
      var executedSequentially = tasks.All(task => task.Result);
      Assert.IsTrue(executedSequentially);
    }

    [TestMethod]
    public async Task Dispatch_When_Called_Inside_Strand_Action_Is_Executed_Inline()
    {
      var originalTaskThreadId = INVALID_THREAD_ID;
      var inDispatchTaskThreadId = INVALID_THREAD_ID;

      var task = TestTaskFactory.StartNew(() =>
                                          {
                                            originalTaskThreadId = Thread.CurrentThread.ManagedThreadId;
                                            m_strandScheduler.Dispatch(() => inDispatchTaskThreadId = Thread.CurrentThread.ManagedThreadId).Wait();
                                          });

      await task;
      Assert.AreNotEqual(INVALID_THREAD_ID, originalTaskThreadId);
      Assert.AreNotEqual(INVALID_THREAD_ID, inDispatchTaskThreadId);
      Assert.AreEqual(originalTaskThreadId, inDispatchTaskThreadId);
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
      var outerTask = TestTaskFactory.StartNew(() =>
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
    public void Wrap_When_Wrapped_Action_Is_Not_Invoked_Then_Original_Action_Does_Not_Execute()
    {
      var wasActionExecuted = false;
      var wrappedAction = m_strandScheduler.Wrap(() => wasActionExecuted = true);
      m_strandScheduler.Dispose();
      Assert.IsFalse(wasActionExecuted);
    }

    [TestMethod]
    public void Wrap_When_Wrapped_Action_Is_Invoked_Then_Original_Action_Executed()
    {
      var wasActionExecuted = false;
      var wrappedAction = m_strandScheduler.Wrap(() => wasActionExecuted = true);

      wrappedAction();

      m_strandScheduler.Dispose();
      Assert.IsTrue(wasActionExecuted);
    }

    [TestMethod]
    public void WrapAsTask_When_Wrapped_Action_Is_Not_Invoked_Then_Original_Action_Does_Not_Execute()
    {
      var wasActionExecuted = false;
      var wrappedAction = m_strandScheduler.WrapAsTask(() => wasActionExecuted = true);

      m_strandScheduler.Dispose();
      Assert.IsFalse(wasActionExecuted);
    }

    [TestMethod]
    public async Task WrapAsTask_When_Wrapped_Action_Is_Invoked_Then_Original_Action_Executed()
    {
      var wasActionExecuted = false;
      var wrappedAction = m_strandScheduler.WrapAsTask(() => wasActionExecuted = true);

      await wrappedAction();

      Assert.IsTrue(wasActionExecuted);
    }

    [TestMethod]
    public async Task Post_When_Inside_Strand_Then_Ordering_Of_Post_Calls_Is_Maintained()
    {
      await postOrderingCommon(insideStrand: true, dispatchMethod: false);
    }

    [TestMethod]
    public async Task Post_When_Outside_Strand_Then_Ordering_Of_Post_Calls_Is_Maintained()
    {
      await postOrderingCommon(insideStrand: false, dispatchMethod: false);
    }

    [TestMethod]
    public async Task Dispatch_When_Outside_Strand_Then_Ordering_Of_Post_Calls_Is_Maintained()
    {
      await postOrderingCommon(insideStrand: false, dispatchMethod: true);
    }

    [TestMethod]
    public async Task Dispatch_When_Outside_Strand_Then_Ordering_Of_Dispatch_First_Post_Second_Calls_Is_Maintained()
    {
      await dispatchPostOrderingCommon(dispatchFirst: true);
    }

    [TestMethod]
    public async Task Dispatch_When_Outside_Strand_Then_Ordering_Of_Post_First_Dispatch_Second_Calls_Is_Maintained()
    {
      await dispatchPostOrderingCommon(dispatchFirst: false);
    }

    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    public void Dispatch_When_Scheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_strandScheduler.Dispose();
      m_strandScheduler.Dispatch(() => {});
    }

    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    public void Post_When_Scheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_strandScheduler.Dispose();
      m_strandScheduler.Post(() => {});
    }

    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    public void Wrap_When_Scheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_strandScheduler.Dispose();
      m_strandScheduler.Wrap(() => {});
    }

    [TestMethod]
    [ExpectedException(typeof (ObjectDisposedException))]
    public void WrapAsTask_When_Scheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_strandScheduler.Dispose();
      m_strandScheduler.WrapAsTask(() => {});
    }

    private async Task postOrderingCommon(bool insideStrand, bool dispatchMethod)
    {
      Task postTask1 = null;
      Task postTask2 = null;

      var outerTaskRunner = insideStrand
        ? (Func<Action, Task>) TestTaskFactory.StartNew
        : Task.Run;

      var innerTaskRunner = dispatchMethod
        ? (Func<Action, Task>) m_strandScheduler.Dispatch
        : m_strandScheduler.Post;

      var executedTasks = new ConcurrentQueue<int?>();

      var outerTask = outerTaskRunner(() =>
                                      {
                                        postTask1 = innerTaskRunner(() => executedTasks.Enqueue(Task.CurrentId));
                                        postTask2 = innerTaskRunner(() => executedTasks.Enqueue(Task.CurrentId));
                                      });

      await outerTask;
      await Task.WhenAll(postTask1, postTask2);

      int? firstCompletedTaskId;
      int? secondCompletedTaskId;
      executedTasks.TryDequeue(out firstCompletedTaskId);
      executedTasks.TryDequeue(out secondCompletedTaskId);

      Assert.AreEqual(postTask1.Id, firstCompletedTaskId);
      Assert.AreEqual(postTask2.Id, secondCompletedTaskId);
    }

    private async Task dispatchPostOrderingCommon(bool dispatchFirst)
    {
      Task postTask1 = null;
      Task postTask2 = null;
      var executedTasks = new ConcurrentQueue<int?>();


      var outerTask = Task.Run(() =>
                               {
                                 postTask1 = dispatchFirst ? m_strandScheduler.Dispatch(() => executedTasks.Enqueue(Task.CurrentId))
                                   : m_strandScheduler.Post(() => executedTasks.Enqueue(Task.CurrentId));

                                 postTask2 = dispatchFirst ? m_strandScheduler.Post(() => executedTasks.Enqueue(Task.CurrentId))
                                   : m_strandScheduler.Dispatch(() => executedTasks.Enqueue(Task.CurrentId));
                               });

      await outerTask;
      await Task.WhenAll(postTask1, postTask2);

      int? firstCompletedTaskId;
      int? secondCompletedTaskId;
      executedTasks.TryDequeue(out firstCompletedTaskId);
      executedTasks.TryDequeue(out secondCompletedTaskId);

      Assert.AreEqual(postTask1.Id, firstCompletedTaskId);
      Assert.AreEqual(postTask2.Id, secondCompletedTaskId);
    }
  }
}