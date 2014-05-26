using System;
using System.Threading.Tasks;
using RStein.Async.Examples.ActorsCore;
using RStein.Async.Misc;
using RStein.Async.Schedulers;

namespace RStein.Async.Examples.Actors
{
  public class AsyncPlayerTest
  {
    public const string RUN_DURATION_MESSAGE_FORMAT = "Total ms: {0}";
    public const string PLAYER_1_NAME = "Tomáš Aquinský";
    public const string PLAYER_2_NAME = "Ioannes Fidanza";
    public const string GAME_1_NAME = "De virtutibus in communi";
    public const string GAME_2_NAME = "De spe";
    private IAsyncPlayer m_player1;
    private IAsyncPlayer m_player2;
    private ProxyEngine m_proxyEngine;

    public AsyncPlayerTest()
    {
      var ioServiceScheduler = new IoServiceScheduler();

      var threadPoolScheduler = new IoServiceThreadPoolScheduler(ioServiceScheduler);
      var externalProxyScheduler = new ProxyScheduler(threadPoolScheduler);
      m_proxyEngine = new ProxyEngine(threadPoolScheduler);
      createActors();
    }

    private void createActors()
    {
      m_player1 = new AsyncPlayer(PLAYER_1_NAME);
      m_player2 = new AsyncPlayer(PLAYER_2_NAME);
      m_player1 = m_proxyEngine.CreateProxy(m_player1);
      m_player2 = m_proxyEngine.CreateProxy(m_player2);
    }

    public virtual async Task Run()
    {
      const int PING_COUNT = Int16.MaxValue;

      var duration = await StopWatchUtils.MeasureActionTime(async () =>
                                                                  {
                                                                    Task firstPlayer = m_player1.Ping(PING_COUNT, m_player2, GAME_1_NAME);
                                                                    Task secondPlayer = m_player2.Ping(PING_COUNT, m_player1, GAME_2_NAME);
                                                                    await Task.WhenAll(firstPlayer, secondPlayer);
                                                                  });

      Console.WriteLine(RUN_DURATION_MESSAGE_FORMAT, duration.TotalMilliseconds);
    }
  }
}