using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.Async.Examples.AsyncConsoleDownloader
{
  public class AsyncDownloaderTester
  {
    private static readonly string _pageSeparator = new string('=', 100);

    public async Task<int> DownloadPages(IEnumerable<string> urls)
    {
      var downloadTasks = (from url in urls
        select new WebClient().DownloadStringTaskAsync(url)).ToList();

      int successfullyCompletedTasks = 0;
      while (downloadTasks.Any())
      {
        Task<String> currentTask = null;
        try
        {
          currentTask = await Task.WhenAny(downloadTasks);
        }
        catch (Exception ex)
        {

          Console.WriteLine(ex);
        }
        
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
      Console.WriteLine(_pageSeparator);
      Console.WriteLine("Current thread: {0}", Thread.CurrentThread.ManagedThreadId);

      if (currentTask.IsFaulted)
      {
        Console.WriteLine(currentTask.Exception);
        return;
      }

      Console.WriteLine(currentTask.Result.Substring(0, MAX_CHARS));
    }
  }
}