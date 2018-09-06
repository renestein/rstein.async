using System;
using System.Threading;
using static System.String;

namespace RStein.Async.Examples.Actors
{
  public class Player : IPlayer
  {
    private readonly string m_name;

    public Player(string name)
    {
      PingCounter = 0;
      m_name = name ?? Empty;
    }

    public int PingCounter
    {
      get;
      private set;
    }

    public virtual void Ping(int pingCount, IPlayer secondPlayer)
    {
      Console.WriteLine("{0} Ping number: {1} tid: {2},", m_name, pingCount, Thread.CurrentThread.ManagedThreadId);
      
      if (pingCount > 0)
      {
        secondPlayer.Ping(pingCount - 1, this);
        PingCounter = PingCounter + 1;
      }
    }
  }
}