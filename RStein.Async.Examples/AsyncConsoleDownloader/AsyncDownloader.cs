using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NLog.Targets;

namespace RStein.Async.Examples.AsyncConsoleDownloader
{
  public class AsyncDownloader
  {
    private static readonly string PAGE_SEPARATOR = new string('=', 100);

    public async Task<int> DownloadPages(IEnumerable<string> urls)
    {
      var downloadTasks = (from url in urls
                           select new WebClient().DownloadStringTaskAsync(url)).ToList();

      int successfullyCompletedTasks = 0;
      while (downloadTasks.Any())
      {
        var currentTask = await Task.WhenAny(downloadTasks);
        downloadTasks.Remove(currentTask);

        if (taskHasresult(currentTask))
        {
          successfullyCompletedTasks++;
        }

        dumpDownloadTask(currentTask);
      }

      return successfullyCompletedTasks;
    }

    private bool taskHasresult(Task<string> currentTask)
    {
      return currentTask.Status == TaskStatus.RanToCompletion;
    }

    private void dumpDownloadTask(Task<String> currentTask)
    {
      const int MAX_CHARS = 500;
      if (currentTask.IsCanceled)
      {
        return;
      }
      Console.WriteLine(PAGE_SEPARATOR);
      Console.WriteLine("Current thread: {0}", Thread.CurrentThread.ManagedThreadId);

      if (currentTask.IsFaulted)
      {
        Console.WriteLine(currentTask.Exception);
      }

      Console.WriteLine(currentTask.Result.Substring(0, MAX_CHARS));
    }
  }
}