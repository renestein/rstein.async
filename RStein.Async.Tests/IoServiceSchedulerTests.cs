using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
      m_scheduler.Dispatch(() => {});
        
      var result = m_scheduler.Run();
      Assert.AreEqual(1, result);
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
        .Select(_ => m_scheduler.Dispatch(() => {})).ToArray();
          
      var executedTasksCount = m_scheduler.Run();
      Assert.AreEqual(NUMBER_OF_SCHEDULED_TASKS, executedTasksCount);

    }
  }
}
