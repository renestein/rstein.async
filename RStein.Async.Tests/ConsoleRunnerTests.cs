using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RStein.Async.ConsoleEx;

namespace RStein.Async.Tests
{
  [TestClass]
  public class ConsoleRunnerTests
  {
    private const int DEFAULT_INT_RESULT = 42;
    private const int DEFAULT_TASK_DELAY_MS = 500;
    private const int INVALID_INT_RESULT = -1;

    [TestInitialize]
    protected virtual void ConsoleRunnerTestsInitialize() {}

    [TestCleanup]
    protected virtual void ConsoleRunnerTestsCleanup() {}

    [TestMethod]
    public void Run_When_Action_Arg_Then_Action_Executed()
    {
      bool wasExecuted = false;
      ConsoleRunner.Run(() =>
                        {
                          wasExecuted = true;
                        });

      Assert.IsTrue(wasExecuted);
    }

    [TestMethod]
    [ExpectedException(typeof (InvalidOperationException))]
    public void Run_When_Action_Arg_And_Exception_Then_Exception_Is_Rethrown_In_Main_Thread()
    {
      ConsoleRunner.Run(() =>
                        {
                          throw new InvalidOperationException();
                        });
    }

    [TestMethod]
    public void Run_When_Func_Arg_Then_Returns_Expected_Value()
    {
      Func<int> intFunc = () => DEFAULT_INT_RESULT;

      int currentResult = ConsoleRunner.Run(intFunc);

      Assert.AreEqual(DEFAULT_INT_RESULT, currentResult);
    }

    [TestMethod]
    [ExpectedException(typeof (InvalidOperationException))]
    public void Run_When_Func_Arg_And_Exception_Then_Exception_Is_Rethrown_In_Main_Thread()
    {
      Func<int> intFunc = () =>
                          {
                            throw new InvalidOperationException();
                          };

      ConsoleRunner.Run(intFunc);
    }

    [TestMethod]
    public void Run_When_Func_Task_Arg_Then_Returns_Expected_Value()
    {
      Func<Task<int>> taskIntFunc = async () =>
                                          {
                                            await Task.Delay(DEFAULT_TASK_DELAY_MS);
                                            return DEFAULT_INT_RESULT;
                                          };

      int currentResult = ConsoleRunner.Run(taskIntFunc);
      Assert.AreEqual(DEFAULT_INT_RESULT, currentResult);
    }

    [TestMethod]
    [ExpectedException(typeof (InvalidOperationException))]
    public void Run_When_Func_Task_Arg_And_Exception_Then_Exception_Is_Rethrown_In_Main_Thread()
    {
      Func<Task<int>> taskIntFunc = async () =>
                                          {
                                            await Task.Delay(DEFAULT_TASK_DELAY_MS);
                                            throw new InvalidOperationException();
                                          };

      int currentResult = ConsoleRunner.Run(taskIntFunc);
    }

    [TestMethod]
    public void Run_When_Func_Task_Arg_And_Inner_Async_Lambdas_Then_Returns_Expected_Value()
    {
      Func<Task<int>> taskIntFunc = async () =>
                                          {
                                            await Task.Run(async () => await Task.Run(() => INVALID_INT_RESULT));
                                            await Task.Delay(DEFAULT_TASK_DELAY_MS);
                                            return DEFAULT_INT_RESULT;
                                          };

      int currentResult = ConsoleRunner.Run(taskIntFunc);
      Assert.AreEqual(DEFAULT_INT_RESULT, currentResult);
    }

    [TestMethod]
    [ExpectedException(typeof (InvalidOperationException))]
    public void Run_When_Func_Task_Arg_And_Exception_In_Inner_Async_Lambdas_Then_Exception_Is_Rethrown_In_Main_Thread()
    {
      Func<Task<int>> taskIntFunc = async () =>
                                          {
                                            await Task.Run(async () => await Task.Run(() =>
                                                                                      {
                                                                                        throw new InvalidOperationException();
                                                                                      }));
                                            await Task.Delay(DEFAULT_TASK_DELAY_MS);
                                            return DEFAULT_INT_RESULT;
                                          };

      int currentResult = ConsoleRunner.Run(taskIntFunc);
    }
  }
}