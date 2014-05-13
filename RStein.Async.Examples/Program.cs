using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RStein.Async.Examples.Actors;
using RStein.Async.Examples.ConcurrentExclusive;
using RStein.Async.Examples.Coroutines;
using RStein.Async.Examples.MapReduceActors;

namespace RStein.Async.Examples
{
  class Program
  {
    static void Main(string[] args)
    {

      testMapReduceActors();
      //testAsyncPlayers();
      //testPlayerActors();
      //testConcurrentExclusiveSchedulers();
      //testCoroutines();
      Console.ReadLine();
    }

    private static void testMapReduceActors()
    {
      var mapReduce = new MapReduceActorTest();
      mapReduce.Run().Wait();
    }

    private static void testAsyncPlayers()
    {
      var testPlayerActorsTask = testAsyncPlayerActors();
      testPlayerActorsTask.Wait();
    }

    private static async Task testAsyncPlayerActors()
    {
      var playerTest = new AsyncPlayerTest();
      await playerTest.Run();
    }

    private static void testPlayerActors()
    {
      var playerTest = new PlayerTest();
      playerTest.Run();
    }

    private static void testCoroutines()
    {
      using (var tester = new LogCoroutineTester())
      {
        tester.Start();
      }
    }

    private static void testConcurrentExclusiveSchedulers()
    {
      const int NUMBER_OF_ITERATIONS = 100;
      var statistics = new ConcurrentExclusiveSimpleIncrementStatistics();

      Enumerable.Range(0, NUMBER_OF_ITERATIONS)
        .ToList().ForEach(_ => statistics.Run());
    }
  }
}
