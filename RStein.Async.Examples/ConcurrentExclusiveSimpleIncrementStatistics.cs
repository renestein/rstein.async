using System;
using RStein.Async.Misc;

namespace RStein.Async.Examples
{
  public class ConcurrentExclusiveSimpleIncrementStatistics
  {
    private ConcurrentExclusiveSimpleIncrementTasks m_engine;


    public const int NUMBER_OF_EXCLUSIVE_TASKS = 100;
    public const int NUMBER_OF_CONCURRENT_TASKS = 10000;

    public ConcurrentExclusiveSimpleIncrementStatistics()
    {
      m_engine = new ConcurrentExclusiveSimpleIncrementTasks();
    }

    public virtual void Run()
    {
      m_engine.RunStrandConcurrentSchedulerTest(NUMBER_OF_CONCURRENT_TASKS, NUMBER_OF_EXCLUSIVE_TASKS);
      m_engine.RunExclusiveConcurrentSchedulerTest(NUMBER_OF_CONCURRENT_TASKS, NUMBER_OF_EXCLUSIVE_TASKS);

      var exclusiveConcurrentSchedulerDuration = StopWatchUtils.MeasureActionTime(() =>
         m_engine.RunExclusiveConcurrentSchedulerTest(NUMBER_OF_CONCURRENT_TASKS, NUMBER_OF_EXCLUSIVE_TASKS).Wait());

      var strandConcurrentSchedulerDuration = StopWatchUtils.MeasureActionTime(() =>
        m_engine.RunStrandConcurrentSchedulerTest(NUMBER_OF_CONCURRENT_TASKS, NUMBER_OF_EXCLUSIVE_TASKS).Wait());


      printResults("exclusiveConcurrentScheduler:", exclusiveConcurrentSchedulerDuration);
      printResults("StrandConcurrentScheduler:", strandConcurrentSchedulerDuration);

    }

    private void printResults(string message, TimeSpan duration)
    {
      Console.Write(message);
      Console.WriteLine(duration.TotalMilliseconds);
    }
  }
}