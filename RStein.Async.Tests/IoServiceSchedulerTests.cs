using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Misc;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{

  [TestClass]
  public class IoServiceSchedulerTests
  {
    public IoServiceSchedulerTests()
    {
    }

    private TestContext testContextInstance;
    private IoServiceScheduler m_scheduler;
    private ExternalProxyScheduler m_proxyScheduler;

    public TestContext TestContext
    {
      get
      {
        return testContextInstance;
      }
      set
      {
        testContextInstance = value;
      }
    }


    [TestInitialize()]
    public void IoServiceSchedulerTestsInitialize()
    {
      m_scheduler = new IoServiceScheduler();
      m_proxyScheduler = new ExternalProxyScheduler(m_scheduler);
    }


    [TestCleanup()]
    public void MyTestCleanup()
    {
      m_proxyScheduler.Dispose();
      m_scheduler = null;
      m_proxyScheduler = null;

    }



    [TestMethod]
    public void Run_When_Zero_Tasks_Added_Then_Returns_Zero()
    {
      var result = m_scheduler.Run();
      Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void Run_When_One_Task_Added_Then_Returns_One()
    {
      const int NUMBER_OF_SCHEDULED_TASKS = 1;
      m_scheduler.Dispatch(() =>
      {
      });

      var result = m_scheduler.Run();
      Assert.AreEqual(NUMBER_OF_SCHEDULED_TASKS, result);
    }

    [TestMethod]
    public void Run_When_One_Task_Added_Then_Task_Is_Executed()
    {
      bool wasTaskCalled = false;
      m_scheduler.Dispatch(() =>
                           {
                             wasTaskCalled = true;
                           });

      m_scheduler.Run();
      Assert.IsTrue(wasTaskCalled);
    }

    [TestMethod]
    public void Run_When_More_Tasks_Added_Then_All_Tasks_Are_Executed()
    {
      bool wasTask1Called = false;
      bool wasTask2Called = false;
      m_scheduler.Dispatch(() =>
      {
        wasTask1Called = true;
      });

      m_scheduler.Dispatch(() =>
      {
        wasTask2Called = true;
      });

      m_scheduler.Run();
      Assert.IsTrue(wasTask1Called && wasTask2Called);

    }

    [TestMethod]
    public void Run_When_Two_Tasks_Added_Then_Returns_Two()
    {
      const int NUMBER_OF_SCHEDULED_TASKS = 2;

      Enumerable.Range(0, NUMBER_OF_SCHEDULED_TASKS)
        .Select(_ => m_scheduler.Dispatch(() =>
        {
        })).ToArray();

      var executedTasksCount = m_scheduler.Run();
      Assert.AreEqual(NUMBER_OF_SCHEDULED_TASKS, executedTasksCount);

    }

    [TestMethod]
    public void Run_When_One_Task_Added_And_Cancel_Work_Then_Returns_One()
    {

      m_scheduler.Dispatch(() =>
      {
      });

      cancelWorkAfterTimeout();
      var result = m_scheduler.Run();
      Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void Run_When_Zero_Tasks_Added_And_Cancel_Work_Then_Returns_Zero()
    {
      cancelWorkAfterTimeout();
      var result = m_scheduler.Run();
      Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void Run_When_One_Task_Added_And_Cancel_Work_Then_Task_Is_Executed()
    {
      bool wasTaskCalled = false;
      m_scheduler.Dispatch(() =>
      {
        wasTaskCalled = true;
      });

      cancelWorkAfterTimeout();
      m_scheduler.Run();
      Assert.IsTrue(wasTaskCalled);
    }


    [TestMethod]
    public void Run_When_More_Tasks_Added_And_Cancel_Work_Then_All_Tasks_Are_Executed()
    {
      bool wasTask1Called = false;
      bool wasTask2Called = false;
      m_scheduler.Dispatch(() =>
      {
        wasTask1Called = true;
      });

      m_scheduler.Dispatch(() =>
      {
        wasTask2Called = true;
      });

      cancelWorkAfterTimeout();
      m_scheduler.Run();
      Assert.IsTrue(wasTask1Called && wasTask2Called);

    }

    [TestMethod]
    public void Run_When_Two_Tasks_Added_And_Cancel_Work_Then_Returns_Two()
    {
      const int NUMBER_OF_SCHEDULED_TASKS = 2;

      Enumerable.Range(0, NUMBER_OF_SCHEDULED_TASKS)
        .Select(_ => m_scheduler.Dispatch(() =>
        {
        })).ToArray();

      cancelWorkAfterTimeout();
      var executedTasksCount = m_scheduler.Run();
      Assert.AreEqual(NUMBER_OF_SCHEDULED_TASKS, executedTasksCount);

    }

    //Unsafe test
    [TestMethod]
    public void Run_When_Work_Exists_And_Zero_Tasks_Then_Method_Does_Not_Return()
    {
      const int WORK_CANCEL_DELAY_MS = 3000;
      const double RUN_MIN_DURATION_S = 2.0;

      var time = StopWatchUtils.MeasureActionTime(() =>
                                                  {
                                                    cancelWorkAfterTimeout(WORK_CANCEL_DELAY_MS);
                                                    m_scheduler.Run();
                                                  });

      Assert.IsTrue(time.TotalSeconds > RUN_MIN_DURATION_S);

    }

    //Unsafe test
    [TestMethod]
    public void Run_When_Work_Canceled_And_Zero_Tasks_Then_Method_Returns_Immediately()
    {
      const double RUN_MAX_DURATION_S = 0.5;

      var work = new Work(m_scheduler);
      work.Dispose();

      var time = StopWatchUtils.MeasureActionTime(() => m_scheduler.Run());

      Assert.IsTrue(time.TotalSeconds < RUN_MAX_DURATION_S);

    }


    [TestMethod]
    public void RunOne_When_One_Task_Added_Then_Returns_One()
    {
      const int NUMBER_OF_SCHEDULED_TASKS = 1;
      m_scheduler.Dispatch(() =>
      {
      });

      var result = m_scheduler.RunOne();
      Assert.AreEqual(NUMBER_OF_SCHEDULED_TASKS, result);
    }

    [TestMethod]
    public void RunOne_When_One_Task_Added_Then_Task_Is_Executed()
    {
      bool wasTaskCalled = false;
      m_scheduler.Dispatch(() =>
      {
        wasTaskCalled = true;
      });

      m_scheduler.Run();
      Assert.IsTrue(wasTaskCalled);
    }

    [TestMethod]
    public void RunOne_When_More_Tasks_Added_Then_Only_First_Task_Is_Executed()
    {
      bool wasTask1Called = false;
      bool wasTask2Called = false;
      m_scheduler.Dispatch(() =>
      {
        wasTask1Called = true;
      });

      m_scheduler.Dispatch(() =>
      {
        wasTask2Called = true;
      });

      m_scheduler.RunOne();
      Assert.IsTrue(wasTask1Called && !wasTask2Called);

    }

    [TestMethod]
    public void RunOne_When_Two_Tasks_Added_Then_Returns_One()
    {
      const int NUMBER_OF_SCHEDULED_TASKS = 2;
      const int NUMBER_OF_RUNNED_TASKS = 1;

      Enumerable.Range(0, NUMBER_OF_SCHEDULED_TASKS)
        .Select(_ => m_scheduler.Dispatch(() =>
        {
        })).ToArray();

      var executedTasksCount = m_scheduler.RunOne();
      Assert.AreEqual(NUMBER_OF_RUNNED_TASKS, executedTasksCount);

    }

    [TestMethod]
    public void RunOne_When_One_Task_Added_And_Cancel_Work_Then_Returns_One()
    {
      const int NUMBER_OF_RUNNED_TASKS = 1;
      m_scheduler.Dispatch(() =>
      {
      });

      cancelWorkAfterTimeout();
      var result = m_scheduler.RunOne();
      Assert.AreEqual(NUMBER_OF_RUNNED_TASKS, result);
    }


    [TestMethod]
    public void RunOne_When_One_Task_Added_And_Cancel_Work_Then_Task_Is_Executed()
    {
      bool wasTaskCalled = false;
      m_scheduler.Dispatch(() =>
      {
        wasTaskCalled = true;
      });

      cancelWorkAfterTimeout();
      m_scheduler.RunOne();
      Assert.IsTrue(wasTaskCalled);
    }


    [TestMethod]
    public void RunOne_When_More_Tasks_Added_And_Cancel_Work_Then_Only_First_Task_Is_Executed()
    {
      bool wasTask1Called = false;
      bool wasTask2Called = false;
      m_scheduler.Dispatch(() =>
      {
        wasTask1Called = true;
      });

      m_scheduler.Dispatch(() =>
      {
        wasTask2Called = true;
      });

      cancelWorkAfterTimeout();
      m_scheduler.RunOne();
      Assert.IsTrue(wasTask1Called && !wasTask2Called);

    }

    [TestMethod]
    public void RunOne_When_Two_Tasks_Added_And_Cancel_Work_Then_Returns_One()
    {
      const int NUMBER_OF_SCHEDULED_TASKS = 2;
      const int RUNNED_TASKS = 1;

      Enumerable.Range(0, NUMBER_OF_SCHEDULED_TASKS)
        .Select(_ => m_scheduler.Dispatch(() =>
        {
        })).ToArray();

      cancelWorkAfterTimeout();
      var executedTasksCount = m_scheduler.RunOne();
      Assert.AreEqual(RUNNED_TASKS, executedTasksCount);

    }
    //Unsafe test
    [TestMethod]
    public void RunOne_When_Zero_Tasks_Then_Method_Does_Not_Return()
    {
      const int SCHEDULE_WORK_AFTER_MS = 3000;
      const double RUN_MIN_DURATION_S = 2.0;

      var time = StopWatchUtils.MeasureActionTime(() =>
      {
        scheduleTaskAfterDelay(SCHEDULE_WORK_AFTER_MS);
        m_scheduler.RunOne();
      });

      Assert.IsTrue(time.TotalSeconds > RUN_MIN_DURATION_S);

    }

    //Unsafe test
    [TestMethod]
    public void RunOne_When_Work_Canceled_And_Zero_Tasks_Then_Method_Does_Not_Return()
    {
      const int SCHEDULE_WORK_AFTER_MS = 3000;
      const double RUN_MIN_DURATION_S = 2.0;

      var work = new Work(m_scheduler);
      work.Dispose();

      var time = StopWatchUtils.MeasureActionTime(() =>
                                                  {
                                                    scheduleTaskAfterDelay(SCHEDULE_WORK_AFTER_MS);
                                                    m_scheduler.RunOne();
                                                  }
     );

      Assert.IsTrue(time.TotalSeconds > RUN_MIN_DURATION_S);

    }

    //Unsafe test
    [TestMethod]
    public void RunOne_When_Zero_Tasks_And_Scheduler_Disposed_Then_Method_Returns_Zero()
    {

      const int STOP_SCHEDULER_AFTER_MS = 3000;
      const int EXPECTED_ZERO_TASKS = 0;
      ThreadPool.QueueUserWorkItem(_ =>
                                   {
                                     Thread.Sleep(STOP_SCHEDULER_AFTER_MS);
                                     m_scheduler.Dispose();
                                   });

      var tasksCount = m_scheduler.RunOne();
      Assert.AreEqual(EXPECTED_ZERO_TASKS, tasksCount);
    }

    [TestMethod]
    public void Run_When_Tasks_Added_And_Scheduler_Disposed_Then_Some_Tasks_Are_Not_Executed()
    {

      const int STOP_SCHEDULER_AFTER_MS = 3000;
      const int SIMULATE_TASK_WORK_INTERVAL_MS = 100;
      const int NUMBER_OF_TASKS = 100;
      Enumerable.Range(0, NUMBER_OF_TASKS).Select(_ => m_scheduler.Dispatch(() => Thread.Sleep(SIMULATE_TASK_WORK_INTERVAL_MS))).ToArray();


      ThreadPool.QueueUserWorkItem(_ =>
      {
        Thread.Sleep(STOP_SCHEDULER_AFTER_MS);
        m_scheduler.Dispose();
      });


      var numberOfExecutedTasks = m_scheduler.Run();
      TestContext.WriteLine("Number of executed tasks: {0}", numberOfExecutedTasks);
      Assert.IsTrue(numberOfExecutedTasks < NUMBER_OF_TASKS);
    }

    [TestMethod]
    public void Poll_When_One_Task_Added_Then_Returns_One()
    {
      const int NUMBER_OF_SCHEDULED_TASKS = 1;
      m_scheduler.Dispatch(() =>
      {
      });

      var result = m_scheduler.Poll();
      Assert.AreEqual(NUMBER_OF_SCHEDULED_TASKS, result);
    }

    [TestMethod]
    public void Poll_When_One_Task_Added_Then_Task_Is_Executed()
    {
      bool wasTaskCalled = false;
      m_scheduler.Dispatch(() =>
      {
        wasTaskCalled = true;
      });

      m_scheduler.Poll();
      Assert.IsTrue(wasTaskCalled);
    }

    [TestMethod]
    public void Poll_When_More_Tasks_Added_Then_All_Tasks_Are_Executed()
    {
      bool wasTask1Called = false;
      bool wasTask2Called = false;
      m_scheduler.Dispatch(() =>
      {
        wasTask1Called = true;
      });

      m_scheduler.Dispatch(() =>
      {
        wasTask2Called = true;
      });

      m_scheduler.Poll();
      Assert.IsTrue(wasTask1Called && wasTask2Called);

    }

    [TestMethod]
    public void Poll_When_Two_Tasks_Added_Then_Returns_Two()
    {
      const int NUMBER_OF_SCHEDULED_TASKS = 2;

      Enumerable.Range(0, NUMBER_OF_SCHEDULED_TASKS)
        .Select(_ => m_scheduler.Dispatch(() =>
        {
        })).ToArray();

      var executedTasksCount = m_scheduler.Poll();
      Assert.AreEqual(NUMBER_OF_SCHEDULED_TASKS, executedTasksCount);

    }


    //Unsafe test
    [TestMethod]
    public void Poll_When_Work_Exists_And_Zero_Tasks_Then_Method_Return_Immediately()
    {

      const int EXPECTED_ZERO_TASKS = 0;
      var work = new Work(m_scheduler);

      var numberOfTasks = m_scheduler.Poll();

      Assert.AreEqual(EXPECTED_ZERO_TASKS, numberOfTasks);

    }

    [TestMethod]
    public void PollOne_When_One_Task_Added_Then_Returns_One()
    {
      const int NUMBER_OF_SCHEDULED_TASKS = 1;
      m_scheduler.Dispatch(() =>
      {
      });

      var result = m_scheduler.PollOne();
      Assert.AreEqual(NUMBER_OF_SCHEDULED_TASKS, result);
    }

    [TestMethod]
    public void PollOne_When_One_Task_Added_Then_Task_Is_Executed()
    {
      bool wasTaskCalled = false;
      m_scheduler.Dispatch(() =>
      {
        wasTaskCalled = true;
      });

      m_scheduler.PollOne();
      Assert.IsTrue(wasTaskCalled);
    }

    [TestMethod]
    public void PollOne_When_More_Tasks_Added_Then_Only_First_Task_Is_Executed()
    {
      bool wasTask1Called = false;
      bool wasTask2Called = false;
      m_scheduler.Dispatch(() =>
      {
        wasTask1Called = true;
      });

      m_scheduler.Dispatch(() =>
      {
        wasTask2Called = true;
      });

      m_scheduler.PollOne();
      Assert.IsTrue(wasTask1Called && !wasTask2Called);

    }

    [TestMethod]
    public void PollOne_When_Two_Tasks_Added_Then_Returns_One()
    {
      const int NUMBER_OF_SCHEDULED_TASKS = 2;
      const int NUMBER_OF_RUNNED_TASKS = 1;

      Enumerable.Range(0, NUMBER_OF_SCHEDULED_TASKS)
        .Select(_ => m_scheduler.Dispatch(() =>
        {
        })).ToArray();

      var executedTasksCount = m_scheduler.PollOne();
      Assert.AreEqual(NUMBER_OF_RUNNED_TASKS, executedTasksCount);

    }

    [TestMethod]
    public void PollOne_When_One_Task_Added_And_Cancel_Work_Then_Returns_One()
    {
      const int NUMBER_OF_RUNNED_TASKS = 1;
      m_scheduler.Dispatch(() =>
      {
      });

      cancelWorkAfterTimeout();
      var result = m_scheduler.PollOne();
      Assert.AreEqual(NUMBER_OF_RUNNED_TASKS, result);
    }


    [TestMethod]
    public void PollOne_When_One_Task_Added_And_Cancel_Work_Then_Task_Is_Executed()
    {
      bool wasTaskCalled = false;
      m_scheduler.Dispatch(() =>
      {
        wasTaskCalled = true;
      });

      cancelWorkAfterTimeout();
      m_scheduler.PollOne();
      Assert.IsTrue(wasTaskCalled);
    }


    [TestMethod]
    public void PollOne_When_More_Tasks_Added_And_Cancel_Work_Then_Only_First_Task_Is_Executed()
    {
      bool wasTask1Called = false;
      bool wasTask2Called = false;
      m_scheduler.Dispatch(() =>
      {
        wasTask1Called = true;
      });

      m_scheduler.Dispatch(() =>
      {
        wasTask2Called = true;
      });

      cancelWorkAfterTimeout();
      m_scheduler.PollOne();
      Assert.IsTrue(wasTask1Called && !wasTask2Called);

    }

    [TestMethod]
    public void PollOne_When_Two_Tasks_Added_And_Cancel_Work_Then_Returns_One()
    {
      const int NUMBER_OF_SCHEDULED_TASKS = 2;
      const int RUNNED_TASKS = 1;

      Enumerable.Range(0, NUMBER_OF_SCHEDULED_TASKS)
        .Select(_ => m_scheduler.Dispatch(() =>
        {
        })).ToArray();

      cancelWorkAfterTimeout();
      var executedTasksCount = m_scheduler.PollOne();
      Assert.AreEqual(RUNNED_TASKS, executedTasksCount);

    }
    //Unsafe test
    [TestMethod]
    public void PollOne_When_Zero_Tasks_Then_Method_Return_Immediately()
    {
      const int EXPECTED_ZERO_TASKS = 0;
      const double RUN_MIN_DURATION_S = 2.0;

      var executedTasks = m_scheduler.PollOne();


      Assert.AreEqual(EXPECTED_ZERO_TASKS, executedTasks);

    }

    [TestMethod]
    public async Task Dispatch_When_Non_Service_Thread_Then_Task_Is_Queued()
    {
      const int INVALID_THREAD_ID = -1;

      var executingThreadId = INVALID_THREAD_ID;
      var dispatchThreadId = Thread.CurrentThread.ManagedThreadId;

      var task = m_scheduler.Dispatch(() => executingThreadId = Thread.CurrentThread.ManagedThreadId);
      ThreadPool.QueueUserWorkItem(_ => m_scheduler.RunOne());
      await task;
      Assert.AreNotEqual(INVALID_THREAD_ID, executingThreadId);
      Assert.AreNotEqual(dispatchThreadId, executingThreadId);
    }

    [TestMethod]
    public async Task Dispatch_When_Service_Thread_Then_Task_Is_Executed_Inline()
    {
      const int INVALID_THREAD_ID = -1;

      var executingThreadId = INVALID_THREAD_ID;
      var dispatchThreadId = INVALID_THREAD_ID;

      var task = m_scheduler.Dispatch(() =>
                                      {
                                        dispatchThreadId = Thread.CurrentThread.ManagedThreadId;
                                        m_scheduler.Dispatch(() => executingThreadId = Thread.CurrentThread.ManagedThreadId).Wait();
                                      });

      ThreadPool.QueueUserWorkItem(_ => m_scheduler.Run());
      await task;
      Assert.AreNotEqual(INVALID_THREAD_ID, dispatchThreadId);
      Assert.AreNotEqual(INVALID_THREAD_ID, executingThreadId);
      Assert.AreEqual(dispatchThreadId, executingThreadId);
    }

    [TestMethod]
    public async Task Post_When_Non_Service_Thread_Then_Task_Is_Queued()
    {
      const int INVALID_THREAD_ID = -1;

      var executingThreadId = INVALID_THREAD_ID;
      var postThreadId = Thread.CurrentThread.ManagedThreadId;

      var task = m_scheduler.Post(() => executingThreadId = Thread.CurrentThread.ManagedThreadId);
      ThreadPool.QueueUserWorkItem(_ => m_scheduler.RunOne());
      await task;
      Assert.AreNotEqual(INVALID_THREAD_ID, executingThreadId);
      Assert.AreNotEqual(postThreadId, executingThreadId);
    }

    [TestMethod]
    public async Task Post_When_Service_Thread_Then_Task_Is_Queued()
    {
      const int INVALID_THREAD_ID = -1;

      var executingThreadId = INVALID_THREAD_ID;
      var dispatchThreadId = INVALID_THREAD_ID;

      var task = m_scheduler.Dispatch(() =>
      {
        dispatchThreadId = Thread.CurrentThread.ManagedThreadId;
        m_scheduler.Post(() => executingThreadId = Thread.CurrentThread.ManagedThreadId).Wait();
      });

      ThreadPool.QueueUserWorkItem(_ => m_scheduler.RunOne());
      m_scheduler.RunOne();
      await task;
      Assert.AreNotEqual(INVALID_THREAD_ID, dispatchThreadId);
      Assert.AreNotEqual(INVALID_THREAD_ID, executingThreadId);
      Assert.AreNotEqual(dispatchThreadId, executingThreadId);
    }

    [TestMethod]
    public void Wrap_When_Invoked_Wrapped_Action_Then_Action_Is_Executed_In_Scheduler_Thread()
    {
      const int INVALID_THREAD_ID = -1;

      var executingThreadId = INVALID_THREAD_ID;
      var runOneThreadId = INVALID_THREAD_ID;
      var wrapCallThread = Thread.CurrentThread.ManagedThreadId;
      var stillWaitForTaskCts = new CancellationTokenSource();

      var wrappedAction = m_scheduler.Wrap(() =>
                                           {
                                             executingThreadId = Thread.CurrentThread.ManagedThreadId;
                                           });

      wrappedAction();
      ThreadPool.QueueUserWorkItem(_ =>
                                   {
                                     runOneThreadId = Thread.CurrentThread.ManagedThreadId;
                                     m_scheduler.RunOne();
                                     stillWaitForTaskCts.Cancel();
                                   });

      stillWaitForTaskCts.Token.WaitHandle.WaitOne();

      Assert.AreNotEqual(executingThreadId, INVALID_THREAD_ID);
      Assert.AreNotEqual(runOneThreadId, INVALID_THREAD_ID);
      Assert.AreNotEqual(wrapCallThread, executingThreadId);
      Assert.AreNotEqual(wrapCallThread, runOneThreadId);
      Assert.AreEqual(runOneThreadId, executingThreadId);
    }

    [TestMethod]
    public async Task WrapAsTask_When_Invoked_Wrapped_Action_Then_Action_Is_Executed_In_Scheduler_Thread()
    {
      const int INVALID_THREAD_ID = -1;

      var executingThreadId = INVALID_THREAD_ID;
      var runOneThreadId = INVALID_THREAD_ID;
      var wrapCallThread = Thread.CurrentThread.ManagedThreadId;

      var wrappedTaskFunc = m_scheduler.WrapAsTask(() =>
      {
        executingThreadId = Thread.CurrentThread.ManagedThreadId;
      });


      ThreadPool.QueueUserWorkItem(_ =>
      {
        runOneThreadId = Thread.CurrentThread.ManagedThreadId;
        m_scheduler.RunOne();
      });

      await wrappedTaskFunc();

      Assert.AreNotEqual(executingThreadId, INVALID_THREAD_ID);
      Assert.AreNotEqual(runOneThreadId, INVALID_THREAD_ID);
      Assert.AreNotEqual(wrapCallThread, executingThreadId);
      Assert.AreNotEqual(wrapCallThread, runOneThreadId);
      Assert.AreEqual(runOneThreadId, executingThreadId);
    }

    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void Run_When_Scheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_scheduler.Dispose();
      m_scheduler.Run();
    }

    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void RunOne_When_Scheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_scheduler.Dispose();
      m_scheduler.RunOne();
    }

    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void Poll_When_Scheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_scheduler.Dispose();
      m_scheduler.Poll();
    }

    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void PollOne_When_Scheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_scheduler.Dispose();
      m_scheduler.PollOne();
    }

    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void Dispatch_When_Scheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_scheduler.Dispose();
      m_scheduler.Dispatch(() =>
                          {
                          });
    }

    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void Post_When_Scheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_scheduler.Dispose();
      m_scheduler.Post(() =>
                        {

                        });

    }

    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void Wrap_When_Scheduler_Disposed_Then_Throws_ObjectDisposedException()
    {

      m_scheduler.Dispose();
      m_scheduler.Wrap(() =>
                      {

                      });

    }

    [TestMethod]
    [ExpectedException(typeof(ObjectDisposedException))]
    public void WrapAsTask_When_Scheduler_Disposed_Then_Throws_ObjectDisposedException()
    {
      m_scheduler.Dispose();
      m_scheduler.WrapAsTask(() =>
                            {
                            });

    }

    [TestMethod]
    public void Dispose_Repeated_Call_Does_Not_Throw()
    {
      m_scheduler.Dispose();
      m_scheduler.Dispose();

    }

    [TestMethod]
    public async Task Run_When_Called_From_Multiple_Threads_Then_All_Tasks_Executed()
    {
      const int NUMBER_OF_SCHEDULED_TASKS = 100;
      const int DEFAULT_TASK_SLEEP = 100;
      const int NUMBER_OF_WORKER_THREAD = 3;
      var countDownEvent = new CountdownEvent(NUMBER_OF_WORKER_THREAD);

      int executedTasks = 0;

      var allTasks = Enumerable.Range(0, NUMBER_OF_SCHEDULED_TASKS).Select(_ => m_scheduler.Post(()=>Thread.Sleep(DEFAULT_TASK_SLEEP))).ToArray();

      Enumerable.Range(0, NUMBER_OF_WORKER_THREAD).Select(_ => ThreadPool.QueueUserWorkItem(__=>
                                                                                            {
                                                                                               int tasksExecutedInThisThread = m_scheduler.Run();
                                                                                               Interlocked.Add(ref executedTasks, tasksExecutedInThisThread);
                                                                                              countDownEvent.Signal();
                                                                                            })).ToArray();

      await Task.WhenAll(allTasks);
      countDownEvent.Wait();

      Assert.AreEqual(NUMBER_OF_SCHEDULED_TASKS, executedTasks);
      
    }

    private void scheduleTaskAfterDelay(int? sleepMs = null)
    {

      const int DEFAULT_SLEEP = 1000;
      var sleepTime = sleepMs ?? DEFAULT_SLEEP;

      ThreadPool.QueueUserWorkItem(_ =>
                                   {
                                     Thread.Sleep(sleepTime);
                                     m_scheduler.Post(() =>
                                     {
                                     });
                                   });
    }


    private void cancelWorkAfterTimeout(int? sleepMs = null)
    {
      const int DEFAULT_SLEEP = 1000;
      var sleepTime = sleepMs ?? DEFAULT_SLEEP;
      var work = new Work(m_scheduler);
      ThreadPool.QueueUserWorkItem(_ =>
                                   {
                                     Thread.Sleep(sleepTime);
                                     work.Dispose();
                                   });
    }
  }
}