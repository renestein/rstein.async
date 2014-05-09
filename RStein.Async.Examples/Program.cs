using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RStein.Async.Examples.ConcurrentExcclusive;
using RStein.Async.Examples.Coroutines;

namespace RStein.Async.Examples
{
  class Program
  {
    static void Main(string[] args)
    {
      //testConcurrentExclusiveSchedulers();
      testCoroutines();
      Console.ReadLine();
    }

    private static void testCoroutines()
    {
      var tester = new LogCoroutineTester();
      tester.Start();      
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
