﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{
  public abstract class IAutonomousSchedulerTests : ITaskSchedulerTests
  {

    private TaskFactory m_currentTaskFactory;

    protected abstract IExternalProxyScheduler ProxyScheduler
    {
      get;
    }

    public TaskFactory CurrentTaskFactory
    {
      get
      {
        return m_currentTaskFactory;
      }
    }
    
    public override void InitializeTest()
    {
      m_currentTaskFactory = new TaskFactory(ProxyScheduler.AsRealScheduler());
      base.InitializeTest();
    }

  
    [TestMethod]
    public async Task WithTaskFactory_When_One_Task_Is_Queued_Then_Task_is_Executed()
    {
      bool wasTaskExecuted = false;
      await CurrentTaskFactory.StartNew(() => wasTaskExecuted = true);

      Assert.IsTrue(wasTaskExecuted);

    }

    [TestMethod]
    public async Task WithTaskFactory_When_Tasks_Are_Queued_Then_All_Tasks_Are_Executed()
    {

      const int NUMBER_OF_TASKS = 10000;
      int numberOfTasksExecuted = 0;

      var tasks = Enumerable.Range(0, NUMBER_OF_TASKS)
        .Select(_ => CurrentTaskFactory.StartNew(() => Interlocked.Increment(ref numberOfTasksExecuted))).ToArray();

      await Task.WhenAll(tasks);

      Assert.AreEqual(NUMBER_OF_TASKS, numberOfTasksExecuted);

    }

    [TestMethod]
    public async Task Dispose_When_Tasks_Are_Queued_Then_All_Tasks_Are_Executed()
    {

      const int NUMBER_OF_TASKS = 1000;
      const int DELAY_TASK_CAN_CONTINUE_SIGNAL_S = 1;

      int numberOfTasksExecuted = 0;
      var waitForSignalCts = new CancellationTokenSource();

      var tasks = Enumerable.Range(0, NUMBER_OF_TASKS)
        .Select(taskIndex => CurrentTaskFactory.StartNew(() =>
                                                    {

                                                      waitForSignalCts.Token.WaitHandle.WaitOne();
                                                      return Interlocked.Increment(ref numberOfTasksExecuted);
                                                    })).ToArray();

      waitForSignalCts.CancelAfter(TimeSpan.FromSeconds(DELAY_TASK_CAN_CONTINUE_SIGNAL_S));
      Scheduler.Dispose();


      await Task.WhenAll(tasks);

      Assert.AreEqual(NUMBER_OF_TASKS, numberOfTasksExecuted);

    }
  }
}