using System;
using System.Threading;
using System.Threading.Tasks;
using static System.String;

namespace RStein.Async.Examples.Actors
{
  public class AsyncPlayer : IAsyncPlayer
  {
    private readonly string m_name;
    private int m_pingCounter;

    public AsyncPlayer(string name)
    {
      m_pingCounter = 0;
      m_name = name ?? Empty;
    }

    public async Task Ping(int pingCount, IAsyncPlayer secondPlayer, string gameName)
    {
      var currentGameName = gameName ?? Empty;

      Console.WriteLine($"{m_name} Ping number: {pingCount} tid: {Thread.CurrentThread.ManagedThreadId} game: {currentGameName}");

      if (pingCount > 0)
      {
        await secondPlayer.Ping(pingCount - 1, this, gameName);
        m_pingCounter++;
      }
      Console.WriteLine($"{m_name} Ping total: {m_pingCounter} tid: {Thread.CurrentThread.ManagedThreadId}, game: {currentGameName}");
    }
  }
}