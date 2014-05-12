using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RStein.Async.Misc
{
  public class StopWatchUtils
  {
    public static TimeSpan MeasureActionTime(Action action)
    {
      var stopwatch = Stopwatch.StartNew();
      action();
      stopwatch.Stop();
      return stopwatch.Elapsed;
    }

    public static async Task<TimeSpan> MeasureActionTime(Func<Task> function)
    {
      var stopwatch = Stopwatch.StartNew();
      await function();
      stopwatch.Stop();
      return stopwatch.Elapsed;
    }
  }
}