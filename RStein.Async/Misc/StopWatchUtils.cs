using System;
using System.Diagnostics;

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
  }
}