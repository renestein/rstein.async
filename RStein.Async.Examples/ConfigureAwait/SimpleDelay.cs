using System.Threading.Tasks;

namespace RStein.Async.Examples.ConfigureAwait
{
  public class SimpleDelay
  {
    private const int DELAY_IN_MS = 5;

    public SimpleDelay()
    {
    }

    public Task Delay()
    {
      return Task.Delay(DELAY_IN_MS);
    }
  }
}