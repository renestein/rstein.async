using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class IoServiceScheduler : TaskScheduler, IDisposable
  {
    public const int REQUIRED_WORK_CANCEL_TOKEN_VALUE = 1;
    private readonly BlockingCollection<Task> m_tasks;
    private volatile int m_workCounter;
    private readonly object m_workLockObject;
    private readonly object m_serviceLockObject;
    private ThreadLocal<bool> m_isServiceThreadMark;
    private CancellationTokenSource m_stopCancelTokenSource;
    private CancellationTokenSource m_workCancelTokenSource;
    private volatile bool m_isDisposed;

    public IoServiceScheduler()
    {
      m_tasks = new BlockingCollection<Task>();
      m_stopCancelTokenSource = new CancellationTokenSource();
      m_workLockObject = new object();
      m_serviceLockObject = new object();
      m_workCounter = 0;
    }


    public virtual void Run()
    {
      checkIfDisposed();
      try
      {
        markCurrentThreadAsServiceThread();
        runAllTasks(isWorkPresent());
      }
      finally
      {
        clearCurrentThreadAsServiceMark();
      }
    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposing)
      {
        doStop();
      }
    }

    public virtual void Dispose()
    {
      lock (m_serviceLockObject)
      {
        if (m_isDisposed)
        {
          return;
        }

        try
        {
          Dispose(false);
          m_isDisposed = true;
          GC.SuppressFinalize(this);
        }
        catch (Exception ex)
        {
          Trace.WriteLine(ex);
        }

      }

    }

    internal void AddWork(Work work)
    {
      if (work == null)
      {
        throw new ArgumentNullException("work");
      }

      handleWorkAdded(work);
    }

    private void doStop()
    {
      m_stopCancelTokenSource.Cancel();
    }

    private void clearCurrentThreadAsServiceMark()
    {
      m_isServiceThreadMark.Value = false;
    }

    private void markCurrentThreadAsServiceThread()
    {
      m_isServiceThreadMark.Value = true;
    }


    private void handleWorkAdded(Work work)
    {
      lock (m_workLockObject)
      {
        m_workCounter++;
        if (isNewWorkCancelTokenRequired())
        {
          Debug.Assert(m_workCancelTokenSource.Token.IsCancellationRequested);
          m_workCancelTokenSource = new CancellationTokenSource();
        }

        work.CancelToken.Register(handleWorkCanceled);
      }
    }

    private bool isNewWorkCancelTokenRequired()
    {
      return (m_workCounter == REQUIRED_WORK_CANCEL_TOKEN_VALUE);
    }

    private void handleWorkCanceled()
    {
      lock (m_workLockObject)
      {
        var cancellationTokenSource = m_workCancelTokenSource;

        m_workCounter--;

        if (m_workCounter == 0)
        {
          cancellationTokenSource.Cancel();
        }
      }
    }

    protected override void QueueTask(Task task)
    {
      m_tasks.Add(task);
      m_isServiceThreadMark = new ThreadLocal<bool>();
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      if (!m_isServiceThreadMark.Value)
      {
        return false;
      }

      TryExecuteTask(task);
      return true;
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
      return m_tasks.ToArray();
    }

    private void runAllTasks(bool waitForWorkCancel = false)
    {
      var usedCancelToken = waitForWorkCancel
        ? CancellationTokenSource.CreateLinkedTokenSource(m_stopCancelTokenSource.Token, m_workCancelTokenSource.Token).Token
        : m_stopCancelTokenSource.Token;

      try
      {
        Task task;
        while (m_tasks.TryTake(out task, Timeout.Infinite, usedCancelToken))
        {
          TryExecuteTask(task);
        }
      }
      catch (OperationCanceledException e)
      {
        Trace.WriteLine(e);
      }
    }

    private bool isWorkPresent()
    {
      lock (m_workLockObject)
      {
        return m_workCounter > 0;
      }
    }

    private void checkIfDisposed()
    {
      if (m_isDisposed)
      {
        throw new ObjectDisposedException(GetType().FullName);
      }
    }

  }
}