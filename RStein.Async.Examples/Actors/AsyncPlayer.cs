using System;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.Async.Examples.Actors
{
  public class AsyncPlayer : IAsyncPlayer
  {
    private readonly string m_name;
    private int m_pingCounter;

    public AsyncPlayer(string name)
    {
      m_pingCounter = 0;
      m_name = name ?? String.Empty;
    }

    public async Task Ping(int pingCount, IAsyncPlayer secondPlayer, string gameName)
    {
      var currentGameName = gameName ?? String.Empty;

      Console.WriteLine("{0} Ping number: {1} tid: {2} game: {3}", m_name, pingCount, Thread.CurrentThread.ManagedThreadId, currentGameName);

      if (pingCount > 0)
      {
        await secondPlayer.Ping(pingCount - 1, this, gameName);
        m_pingCounter++;
      }
      Console.WriteLine("{0} Ping total: {1} tid: {2}, game: {3}", m_name, m_pingCounter, Thread.CurrentThread.ManagedThreadId, currentGameName);
    }
  }
}