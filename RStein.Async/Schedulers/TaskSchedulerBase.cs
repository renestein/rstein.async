using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public abstract class TaskSchedulerBase : ITaskScheduler
  {
    private const string PROXY_SCHEDULER_ALREADY_SET_EXCEPTION_MESSAGE = "ProxyScheduler is already set and cannnot be modified!";

    private bool m_disposed;
    private readonly object m_serviceLockObject;
    private readonly TaskCompletionSource<object> m_serviceCompletetcs;
    private IExternalProxyScheduler m_proxyScheduler;
    private CancellationTokenSource m_schedulerCancellationTokenSource;

    protected TaskSchedulerBase()
    {
      m_disposed = false;
      m_serviceLockObject = new Object();
      m_serviceCompletetcs = new TaskCompletionSource<object>();
    }

    protected object GetServiceLockObject
    {
      get
      {
        return m_serviceLockObject;
      }
    }

    public abstract int MaximumConcurrencyLevel
    {
      get;
    }

    public virtual IExternalProxyScheduler ProxyScheduler
    {
      get
      {
        Debug.Assert(m_proxyScheduler != null);
        return m_proxyScheduler;
      }

      set
      {
        lock (GetServiceLockObject)
        {
          checkIfDisposed();
          if (value == null)
          {
            throw new ArgumentNullException("scheduler");
          }

          if (m_proxyScheduler != null)
          {
            throw new InvalidOperationException(PROXY_SCHEDULER_ALREADY_SET_EXCEPTION_MESSAGE);
          }

          m_proxyScheduler = value;
        }
      }
    }

    public virtual Task Complete
    {
      get
      {
        return m_serviceCompletetcs.Task;
      }
    }

    public virtual CancellationToken SchedulerRunCanceledToken
    {
      get
      {
        return m_schedulerCancellationTokenSource.Token;
      }
    }

    protected virtual CancellationTokenSource SchedulerRunCancellationTokenSource
    {
      get
      {
        return m_schedulerCancellationTokenSource;
      }
    }

    public void Dispose()
    {
      lock (m_serviceLockObject)
      {
        if (m_disposed)
        {
          return;
        }

        try
        {
          Dispose(true);
          m_disposed = true;
          m_serviceCompletetcs.TrySetResult(null);
        }
        catch (Exception ex)
        {
          Trace.WriteLine(ex);
          m_serviceCompletetcs.TrySetException(ex);
        }

      }


    }
    public abstract void QueueTask(Task task);
    public abstract bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued);
    public abstract IEnumerable<Task> GetScheduledTasks();

    protected abstract void Dispose(bool disposing);

    protected void checkIfDisposed()
    {
      if (m_disposed)
      {
        throw new ObjectDisposedException(GetType().FullName);
      }
    }
  }
}