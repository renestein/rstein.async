﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RStein.Async.ConsoleEx;
using RStein.Async.Examples.Actors;
using RStein.Async.Examples.AsyncConsoleDownloader;
using RStein.Async.Examples.BrokenPromise;
using RStein.Async.Examples.ConcurrentExclusive;
using RStein.Async.Examples.ConfigureAwait;
using RStein.Async.Examples.Coroutines;
using RStein.Async.Examples.LeftRight;
using RStein.Async.Examples.MapReduceActors;
using RStein.Async.Tasks;

namespace RStein.Async.Examples
{
  internal class Program
  {
    private static readonly string[] _urls =
    {
      "http://twitter.com",
      "http://google.com",
      "http://msdn.microsoft.com",
      "http://www.zive.cz",
      "http://dummy_xxxxxx.aaaa"
    };

    private static void Main(string[] args)
    {
      //testBrokenPromises();
      //testDownloadPages();
      testMapReduceActors();
      //testAsyncPlayers();
      //testPlayerActors();
      //testConcurrentExclusiveSchedulers();
      //testCoroutines();
      //testLeftRightList();

      //testConfigureAwaitTester();
      //runTestSoftPcAwaiter();
      Console.ReadLine();
    }

    //private static async Task runTestSoftPcAwaiter()
    //{
    //  var tester = new Spc_Await_Tester();
    //  ConsoleRunner.Run(()=>tester.TestAwaiter());
    //}

    private static void testConfigureAwaitTester()
    {
      ConsoleRunner.Run(ConfigureAwaitTester.Run);
    }

    private static void testLeftRightList()
    {
      var leftRightLists = new LeftRightList();
      var cts = new CancellationTokenSource();

      Task.Run(()=>leftRightLists.Execute(cts.Token), cts.Token);
      Console.Read();
      cts.Cancel();
      Console.Read();
    }

    private static void testBrokenPromises()
    {
      var leakTaskCompletionSource = new LeakTaskCompletionSource();
      var task = leakTaskCompletionSource.Leak();
      DebugTaskCompletionSourceServices.DetectBrokenTaskCompletionSources();
    }

    private static void testDownloadPages()
    {
      Console.WriteLine("Main: Current thread {0}", Thread.CurrentThread.ManagedThreadId);
      var successfulTasks = ConsoleRunner.Run(DownloadWebPages);
      //int successfulTasks = DownloadWebPages().Result;
      Console.WriteLine("Number of successful downloads: {0} Total urls: {1}", successfulTasks, _urls.Count());
    }

    private static async Task<int> DownloadWebPages()
    {
      var downloader = new AsyncDownloaderTester();
      var successfulTasks = await downloader.DownloadPages(_urls);
      return successfulTasks;

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