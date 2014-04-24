﻿using System;
using System.Linq;
using System.Threading;
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
    }


    [TestCleanup()]
    public void MyTestCleanup()
    {
      m_scheduler = null;
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
    public void RunOne_When_Work_Canceled_And_Zero_Tasks_Then_Method_DoesNotReturn()
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
    public void RunOne_When_And_Zero_Tasks_And_Scheduler_Disposed_Then_Method_Returns_Zero()
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