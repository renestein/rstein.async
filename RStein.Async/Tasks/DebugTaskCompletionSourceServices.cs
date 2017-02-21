using System;
using System.Diagnostics;
using System.Runtime;

namespace RStein.Async.Tasks
{
  public class DebugTaskCompletionSourceServices
  {
    [Conditional("DEBUG")]
    public static void DetectBrokenTaskCompletionSources()
    {      
      collect();
      GC.WaitForPendingFinalizers();
      collect();
    }

    private static void collect()
    {
      GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
    }
  }
}