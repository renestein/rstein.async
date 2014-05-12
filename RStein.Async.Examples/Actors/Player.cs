using System;
using System.Threading;

namespace RStein.Async.Examples.Actors
{
  public class Player
  {
    private int m_pingCounter;
    private string m_name;

    public Player(string name)
    {
      m_pingCounter = 0;
      m_name = name ?? String.Empty;
    }

    public virtual void Ping(int pingCount, Player secondPlayer)
    {
      Console.WriteLine("{0} Ping: tid {1},", m_name, Thread.CurrentThread.ManagedThreadId);
      secondPlayer.Ping(--pingCount, this);
      m_pingCounter++;
    }
  }
}