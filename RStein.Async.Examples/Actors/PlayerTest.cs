using RStein.Async.Actors.ActorsCore;
using RStein.Async.Schedulers;

namespace RStein.Async.Examples.Actors
{
  public class PlayerTest
  {
    public const string PLAYER_1_NAME = "Tomáš Aquinský";
    public const string PLAYER_2_NAME = "Siger Brabantský";
    private const int NUMBER_OF_THREADS = 10;
    private readonly ProxyEngine m_proxyEngine;
    private IPlayer m_player1;
    private IPlayer m_player2;

    public PlayerTest()
    {
      var ioServiceScheduler = new IoServiceScheduler();

      var threadPoolScheduler = new IoServiceThreadPoolScheduler(ioServiceScheduler, NUMBER_OF_THREADS);
      var externalProxyScheduler = new ProxyScheduler(threadPoolScheduler);
      m_proxyEngine = new ProxyEngine(threadPoolScheduler);
      createActors();
    }

    public virtual void Run()
    {
      const int PING_COUNT = 1000;
      m_player1.Ping(PING_COUNT, m_player2);
    }

    private void createActors()
    {
      m_player1 = new Player(PLAYER_1_NAME);
      m_player2 = new Player(PLAYER_2_NAME);
      m_player1 = m_proxyEngine.CreateProxy(m_player1);
      m_player2 = m_proxyEngine.CreateProxy(m_player2);
    }
  }
}