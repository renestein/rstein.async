﻿using System;
using System.Threading;

namespace RStein.Async.Examples.Actors
{
  public class Player : IPlayer
  {
    private readonly string m_name;
    private int m_pingCounter;

    public Player(string name)
    {
      m_pingCounter = 0;
      m_name = name ?? String.Empty;
    }

    public int PingCounter
    {
      get
      {
        return m_pingCounter;
      }
    }

    public virtual void Ping(int pingCount, IPlayer secondPlayer)
    {
      Console.WriteLine("{0} Ping number: {1} tid: {2},", m_name, pingCount, Thread.CurrentThread.ManagedThreadId);

      if (pingCount > 0)
      {
        secondPlayer.Ping(pingCount - 1, this);
        m_pingCounter = PingCounter + 1;
      }
    }
  }
}