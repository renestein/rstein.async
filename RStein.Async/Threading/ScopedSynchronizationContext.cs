using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading;

namespace RStein.Async.Threading
{
  public sealed class ScopedSynchronizationContext : IDisposable
  {
    private readonly SynchronizationContext m_newSynchronizationContext;
    private SynchronizationContext m_oldContext;

    public ScopedSynchronizationContext(SynchronizationContext newSynchronizationContext)
    {
      m_newSynchronizationContext = newSynchronizationContext;
      setNewContext();
    }

    private void setNewContext()
    {
      m_oldContext = SynchronizationContext.Current;
      SynchronizationContext.SetSynchronizationContext(m_newSynchronizationContext);
    }

    public void Dispose()
    {
      Dispose(true);
    }

    private void Dispose(bool disposing)
    {
      if (disposing)
      {
        var currentContext = SynchronizationContext.Current;
        Debug.Assert(ReferenceEquals(currentContext, m_newSynchronizationContext));
        SynchronizationContext.SetSynchronizationContext(m_oldContext);
      }
    }
  }
}