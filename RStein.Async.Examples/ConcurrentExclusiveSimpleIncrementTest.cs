using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RStein.Async.Schedulers;

namespace RStein.Async.Examples
{
  public class ConcurrentExclusiveSimpleIncrementTest
  {
    public Task RunStrandConcurrentSchedulerTest(int numberOfConcurrentRasks,
                                                 int numberOfExclusiveTasks)
    {
      var schedulerPair = new ConcurrentStrandSchedulerPair(Environment.ProcessorCount);
      return runTasks(schedulerPair.ConcurrentScheduler, 
                     schedulerPair.StrandScheduler, 
                     numberOfConcurrentRasks, 
                     numberOfExclusiveTasks);

    }

    public Task RunExclusiveConcurrentSchedulerTest(int numberOfConcurrentRasks,
                                                    int numberOfExclusiveTasks)
    {
      var schedulerPair = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, Environment.ProcessorCount);
      return runTasks(schedulerPair.ConcurrentScheduler,
                     schedulerPair.ExclusiveScheduler,
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

      int concurrentVariable = 0;
      var concurrentTaskFactory = new TaskFactory(concurrentScheduler);
      var exclusiveFactory = new TaskFactory(exclusiveScheduler);

      int numberOfProducers = numberOfConcurrentTasks / BATCH_SIZE;
      numberOfProducers = (numberOfConcurrentTasks % BATCH_SIZE == 0 ? numberOfProducers : numberOfProducers + 1);
      var concurrentTasks = new ConcurrentBag<Task>();

      Parallel.For(0, numberOfProducers, producedIndex =>
                                         {
                                           int startIndex = producedIndex;
                                           int endIndex = Math.Min(startIndex + BATCH_SIZE, numberOfConcurrentTasks);
                                           for (int i = startIndex; i < endIndex; i++)
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