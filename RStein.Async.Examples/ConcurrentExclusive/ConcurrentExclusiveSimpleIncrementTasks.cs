using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RStein.Async.Schedulers;

namespace RStein.Async.Examples.ConcurrentExclusive
{
  public class ConcurrentExclusiveSimpleIncrementTasks
  {
    private readonly ConcurrentExclusiveSchedulerPair m_concurrentExclusiveSchedulerPair;
    private readonly ConcurrentStrandSchedulerPair m_strandExclusiveSchedulerPair;

    public ConcurrentExclusiveSimpleIncrementTasks()
    {
      m_strandExclusiveSchedulerPair = new ConcurrentStrandSchedulerPair(Environment.ProcessorCount);
      m_concurrentExclusiveSchedulerPair = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, Environment.ProcessorCount);
    }

    public Task RunStrandConcurrentSchedulerTest(int numberOfConcurrentTasks,
      int numberOfExclusiveTasks)
    {
      return runTasks(m_strandExclusiveSchedulerPair.ConcurrentScheduler,
        m_strandExclusiveSchedulerPair.StrandScheduler,
        numberOfConcurrentTasks,
        numberOfExclusiveTasks);
    }

    public Task RunExclusiveConcurrentSchedulerTest(int numberOfConcurrentRasks,
      int numberOfExclusiveTasks)
    {
      return runTasks(m_concurrentExclusiveSchedulerPair.ConcurrentScheduler,
        m_concurrentExclusiveSchedulerPair.ExclusiveScheduler,
        numberOfConcurrentRasks,
        numberOfExclusiveTasks);
    }

    private Task runTasks(TaskScheduler concurrentScheduler,
      TaskScheduler exclusiveScheduler,
      int numberOfConcurrentTasks,
      int numberOfExclusiveTasks)
    {
      if (numberOfConcurrentTasks <= 0)
      {
        throw new ArgumentException("numberOfConcurrentTasks");
      }

      if (numberOfExclusiveTasks <= 0)
      {
        throw new ArgumentException("numberOfExclusiveTasks");
      }

      const int BATCH_SIZE = 5;

      var concurrentVariable = 0;
      var concurrentTaskFactory = new TaskFactory(concurrentScheduler);
      var exclusiveFactory = new TaskFactory(exclusiveScheduler);

      var numberOfProducers = numberOfConcurrentTasks/BATCH_SIZE;
      numberOfProducers = (numberOfConcurrentTasks%BATCH_SIZE == 0 ? numberOfProducers : numberOfProducers + 1);
      var concurrentTasks = new ConcurrentBag<Task>();

      Parallel.For(0, numberOfProducers, producedIndex =>
                                         {
                                           var startIndex = producedIndex;
                                           var endIndex = Math.Min(startIndex + BATCH_SIZE, numberOfConcurrentTasks);
                                           for (var i = startIndex; i < endIndex; i++)
                                           {
                                             var task = concurrentTaskFactory.StartNew(() => Interlocked.Increment(ref concurrentVariable));
                                             concurrentTasks.Add(task);
                                           }
                                         });

      var exclusiveTasksArray = Enumerable.Range(0, numberOfExclusiveTasks)
        .Select(index => exclusiveFactory.StartNew(() =>
                                                   {
                                                     var currentValue = concurrentVariable;
                                                     var oldResult = Interlocked.CompareExchange(ref concurrentVariable, index, currentValue);
                                                     Debug.Assert(currentValue == oldResult);
                                                   })).ToArray();

      return Task.WhenAll(concurrentTasks.Union(exclusiveTasksArray).ToArray());
    }
  }
}