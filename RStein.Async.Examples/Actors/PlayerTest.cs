using System.Net.NetworkInformation;
using RStein.Async.Examples.ActorsCore;
using RStein.Async.Schedulers;

namespace RStein.Async.Examples.Actors
{
  public class PlayerTest
  {
    public const string PLAYER_1_NAME = "Tomáš Aquinský";
    public const string PLAYER_2_NAME = "Siger Brabantský";
    private ProxyEngine m_proxyEngine;
    private IPlayer player1;
    private IPlayer player2;

    public PlayerTest()
    {
      var ioServiceScheduler = new IoServiceScheduler();
      
      var threadPoolScheduler = new IoServiceThreadPoolScheduler(ioServiceScheduler);
      var externalProxyScheduler = new ExternalProxyScheduler(threadPoolScheduler);
      m_proxyEngine = new ProxyEngine(threadPoolScheduler);
      createActors();
    }

    private void createActors()
    {
      player1 = new Player(PLAYER_1_NAME);
      player2 = new Player(PLAYER_2_NAME);
      player1 = m_proxyEngine.CreateProxy<IPlayer>(player1);
      player2 = m_proxyEngine.CreateProxy<IPlayer>(player2);
    }

    public virtual void Run()
    {
      const int PING_COUNT = 10000;
      player1.Ping(PING_COUNT, player2);
    }
  }
}