using System;
using System.Threading.Tasks;

namespace RStein.Async.Examples.SoftPcConfigureAwait
{
  public class Spc_Await_Tester
  {
    public async Task TestAwaiter()
    {
      const int DELAY_IN_MS = 2000;

      Console.WriteLine("Without ConfigureAwait");
      await Task.Delay(DELAY_IN_MS);

      Console.WriteLine("With ConfigureAwait true");
      await Task.Delay(DELAY_IN_MS).ConfigureAwait(true);

      Console.WriteLine("With ConfigureAwait false");
      await Task.Delay(DELAY_IN_MS).ConfigureAwait(false);

      Console.WriteLine("Without ConfigureAwait");
      await Task.Delay(DELAY_IN_MS);
    }
  }
}