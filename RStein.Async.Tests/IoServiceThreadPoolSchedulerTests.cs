using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.Schedulers;

namespace RStein.Async.Tests
{
  [TestClass]
  public class IoServiceThreadPoolSchedulerTests : ITaskSchedulerTests
  {
    private IoServiceScheduler m_ioService;
    private IoServiceThreadPoolScheduler m_threadPool;
    private TaskFactory m_taskfactory;
    private ExternalProxyScheduler m_externalScheduler;
    
    protected override ITaskScheduler Scheduler
    {
      get
      {
        return m_threadPool;
      }
    }


    [TestInitializeAttribute]
    public void IoServiceThreadPoolSchedulerTestsInitialize()
    {
      m_ioService = new IoServiceScheduler();
      m_threadPool = new IoServiceThreadPoolScheduler(m_ioService);
      m_externalScheduler = new ExternalProxyScheduler(m_threadPool);
      m_taskfactory = new TaskFactory(m_externalScheduler);
    }

    [TestCleanup]
    public void IoServiceThreadPoolSchedulerTestsCleanup()
    {
      m_threadPool.Dispose();
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Ctor_When_Io_Service_Is_Null_Then_Throws_ArgumentException()
    {
      var threadPool = new IoServiceThreadPoolScheduler(null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Ctor_When_Number_Of_Threads_Is_Zero_Then_Throws_ArgumentOutOfRangeException()
    {
      var threadPool = new IoServiceThreadPoolScheduler(m_ioService, 0);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Ctor_When_Number_Of_Threads_Is_Negative_Then_Throws_ArgumentOutOfRangeException()
    {
      var threadPool = new IoServiceThreadPoolScheduler(m_ioService, -1);
    }

    [TestMethod]
    public async Task WithTaskFactory_When_One_Task_Is_Queued_Then_Task_is_Executed()
    {
      bool wasTaskExecuted = false;
      await m_taskfactory.StartNew(() => wasTaskExecuted = true);

      Assert.IsTrue(wasTaskExecuted);

    }

    [TestMethod]
    public async Task WithTaskFactory_When_Tasks_Are_Queued_Then_All_Tasks_Are_Executed()
    {

      const int NUMBER_OF_TASKS = 1000;
      int numberOfTasksExecuted = 0;

      var tasks = Enumerable.Range(0, NUMBER_OF_TASKS)
                  .Select(_ => m_taskfactory.StartNew(() => Interlocked.Increment(ref numberOfTasksExecuted))).ToArray();

      await Task.WhenAll(tasks);

      Assert.AreEqual(NUMBER_OF_TASKS, numberOfTasksExecuted);

    }

    [TestMethod]
    public async Task Dispose_When_Tasks_Are_Queued_Then_All_Tasks_Are_Executed()
    {

      const int NUMBER_OF_TASKS = 1000000;
      const int DELAY_TASK_CAN_CONTINUE_SIGNAL_S = 1;

      int numberOfTasksExecuted = 0;      
      var waitForSignalCts = new CancellationTokenSource();

      var tasks = Enumerable.Range(0, NUMBER_OF_TASKS)
                  .Select(taskIndex => m_taskfactory.StartNew(() =>
                                                              {

                                                                waitForSignalCts.Token.WaitHandle.WaitOne();
                                                                return Interlocked.Increment(ref numberOfTasksExecuted);
                                                      })).ToArray();

      waitForSignalCts.CancelAfter(TimeSpan.FromSeconds(DELAY_TASK_CAN_CONTINUE_SIGNAL_S));
      m_threadPool.Dispose();
      
      
      await Task.WhenAll(tasks);

      Assert.AreEqual(NUMBER_OF_TASKS, numberOfTasksExecuted);

    }
  }
}
