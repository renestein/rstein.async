using System;
using System.Threading;
using System.Threading.Tasks;
using RStein.Async.Schedulers;
using RStein.Async.Threading;

namespace RStein.Async.Examples.ConfigureAwait
{
  public class ConfigureAwaitTester
  {
    public static async Task<int> Run()
    {
      var delayObj = new SimpleDelay();
        Console.WriteLine("First await - WITHOUT ConfigureAwait");
        await delayObj.Delay();
        Console.WriteLine("Second await - WITH ConfigureAwait=false");
        await delayObj.Delay().ConfigureAwait(continueOnCapturedContext: false);
        Console.WriteLine("Third await - WITH ConfigureAwait=false");
        await delayObj.Delay().ConfigureAwait(continueOnCapturedContext: false);
         Console.WriteLine("Last await - with ConfigureAwait = true");
        await delayObj.Delay().ConfigureAwait(continueOnCapturedContext: true);
        return 0;
    }
  }
}