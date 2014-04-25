﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class IoServiceScheduler : ITaskScheduler, IDisposable
  {
    public const int REQUIRED_WORK_CANCEL_TOKEN_VALUE = 1;
    public const int POLLONE_RUNONE_MAX_TASKS = 1;
    public const int UNLIMITED_MAX_TASKS = -1;

    private readonly BlockingCollection<Task> m_tasks;
    private volatile int m_workCounter;
    private readonly object m_workLockObject;
    private readonly object m_serviceLockObject;
    private readonly ThreadLocal<IoSchedulerThreadServiceFlags> m_isServiceThreadFlags;
    private readonly CancellationTokenSource m_stopCancelTokenSource;
    private CancellationTokenSource m_workCancelTokenSource;
    private volatile bool m_isDisposed;
    private IExternalProxyScheduler m_proxyScheduler;

    public IoServiceScheduler()
    {
      m_tasks = new BlockingCollection<Task>();
      m_isServiceThreadFlags = new ThreadLocal<IoSchedulerThreadServiceFlags>(() => new IoSchedulerThreadServiceFlags());
      m_stopCancelTokenSource = new CancellationTokenSource();
      m_workLockObject = new object();
      m_serviceLockObject = new object();
      m_workCounter = 0;
    }

    public int MaximumConcurrencyLevel
    {
      get
      {
        return Int32.MaxValue;
      }
    }

    public virtual int Run()
    {
      checkIfDisposed();
      return runTasks(withWorkCancelToken());
    }

    public virtual int RunOne()
    {
      checkIfDisposed();
      return runTasks(withGlobalCancelToken(), POLLONE_RUNONE_MAX_TASKS);

    }

    public virtual Task Dispatch(Action action)
    {
      checkIfDisposed();
      if (action == null)
      {
        throw new ArgumentNullException("action");
      }            

      var task = Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, getTplScheduler());
      return task;
    }

    public virtual int Poll()
    {
      checkIfDisposed();
      return runTasks(withoutCancelToken());
    }

    public virtual int PollOne()
    {
      checkIfDisposed();
      return runTasks(withoutCancelToken(), maxTasks: POLLONE_RUNONE_MAX_TASKS);
    }

    public virtual Task Post(Action action)
    {
      checkIfDisposed();

      if (action == null)
      {
        throw new ArgumentNullException("action");
      }

      bool oldIsInServiceThread = m_isServiceThreadFlags.Value.IsServiceThread;

      try
      {
        clearCurrentThreadAsServiceFlag();
        return Dispatch(action);
      }
      finally
      {
        m_isServiceThreadFlags.Value.IsServiceThread = oldIsInServiceThread;
      }

    }

    public virtual Action Wrap(Action action)
    {
      checkIfDisposed();
      if (action == null)
      {
        throw new ArgumentNullException("action");
      }

      return () => Dispatch(action);
    }

    public virtual Func<Task> WrapAsTask(Action action)
    {
      checkIfDisposed();
      if (action == null)
      {
        throw new ArgumentNullException("action");
      }

      return () => Dispatch(action);
    }


    public void Dispose()
    {
      lock (m_serviceLockObject)
      {
        if (m_isDisposed)
        {
          return;
        }

        try
        {
          Dispose(true);
          m_isDisposed = true;

        }
        catch (Exception ex)
        {
          Trace.WriteLine(ex);
        }

      }

    }

    protected virtual void Dispose(bool disposing)
    {
      if (disposing)
      {
        doStop();
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

    private TaskScheduler getTplScheduler()
    {
      Debug.Assert(m_proxyScheduler != null);
      return m_proxyScheduler as TaskScheduler;
    }

    private bool isInServiceThread()
    {
      return m_isServiceThreadFlags.Value.IsServiceThread;
    }

    private void doStop()
    {
      m_stopCancelTokenSource.Cancel();
      handleWorkCanceled(cancelNow: true);
    }

    private void setCurrentThreadAsServiceAllFlags(int maxTasks)
    {
      resetThreadAsServiceAllFlags();
      setThreadAsServiceFlag();
      m_isServiceThreadFlags.Value.MaxOperationsAllowed = maxTasks;
    }

    private void setThreadAsServiceFlag()
    {
      m_isServiceThreadFlags.Value.IsServiceThread = true;
    }

    private void resetThreadAsServiceAllFlags()
    {
      m_isServiceThreadFlags.Value.ResetData();
    }

    private void clearCurrentThreadAsServiceFlag()
    {
      m_isServiceThreadFlags.Value.IsServiceThread = false;
    }

    private void handleWorkAdded(Work work)
    {
      lock (m_workLockObject)
      {
        m_workCounter++;
        if (isNewWorkCancelTokenRequired())
        {
          Debug.Assert(m_workCancelTokenSource == null ||
                      m_workCancelTokenSource.Token.IsCancellationRequested);

          m_workCancelTokenSource = new CancellationTokenSource();
        }

        work.CancelToken.Register(() => handleWorkCanceled());
      }
    }

    private bool isNewWorkCancelTokenRequired()
    {
      return (m_workCounter == REQUIRED_WORK_CANCEL_TOKEN_VALUE);
    }

    private void handleWorkCanceled(bool cancelNow = false)
    {
      lock (m_workLockObject)
      {
        var cancellationTokenSource = m_workCancelTokenSource;

        if (cancelNow)
        {
          if (cancellationTokenSource != null)
          {
            cancellationTokenSource.Cancel();
          }

          return;
        }

        m_workCounter--;

        if (m_workCounter == 0)
        {
          cancellationTokenSource.Cancel();
        }
      }
    }

    public virtual void QueueTask(Task task)
    {
      m_tasks.Add(task);
     

    }

    public virtual bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {

      if (!isInServiceThread())
      {
        return false;
      }

      if (tasksLimitReached())
      {
        return false;
      }

      bool taskExecutedNow = false;
      try
      {
        m_isServiceThreadFlags.Value.ExecutedOperationsCount++;
        taskExecutedNow = m_proxyScheduler.DoTryExecuteTask(task);        
      }
      finally
      {
        if (!taskExecutedNow)
        {
          m_isServiceThreadFlags.Value.ExecutedOperationsCount--;
        }
      }

      return taskExecutedNow;
    }

    public virtual IEnumerable<Task> GetScheduledTasks()
    {
      return m_tasks.ToArray();
    }

    public int MaximumconcurrencyLevel
    {
      get
      {
        return Int32.MaxValue;
      }
    }

    public void SetProxyScheduler(IExternalProxyScheduler scheduler)
    {
      if (scheduler == null)
      {
        throw new ArgumentNullException("scheduler");
      }

      m_proxyScheduler = scheduler;
    }

    private int runTasks(CancellationToken cancellationToken, int maxTasks = UNLIMITED_MAX_TASKS)
    {
      try
      {
        setCurrentThreadAsServiceAllFlags(maxTasks);
        return runTasksCore(cancellationToken);
      }
      finally
      {

        resetThreadAsServiceAllFlags();
      }
    }

    private int runTasksCore(CancellationToken cancellationToken)
    {

      bool searchForTask = true;

      var usedCancellationToken = cancellationToken;
      var serviceData = m_isServiceThreadFlags.Value;

      while (searchForTask)
      {
        searchForTask = false;
        m_stopCancelTokenSource.Token.ThrowIfCancellationRequested();

        try
        {
          Task task;
          if (!tryGetTask(usedCancellationToken, out task))
          {
            continue;
          }

          m_stopCancelTokenSource.Token.ThrowIfCancellationRequested();
          searchForTask = TryExecuteTaskInline(task, true) && !tasksLimitReached();

          m_stopCancelTokenSource.Token.ThrowIfCancellationRequested();
        }

        catch (OperationCanceledException e)
        {
          Trace.WriteLine(e);

          if (m_stopCancelTokenSource.IsCancellationRequested)
          {
            break;
          }

          usedCancellationToken = CancellationToken.None;
          searchForTask = !tasksLimitReached();
        }
      }


      return serviceData.ExecutedOperationsCount;
    }

    private bool tryGetTask(CancellationToken cancellationToken, out Task task)
    {
      if (cancellationToken != CancellationToken.None)
      {
        return m_tasks.TryTake(out task, Timeout.Infinite, cancellationToken);
      }

      return m_tasks.TryTake(out task);
    }

    private bool tasksLimitReached()
    {
      var serviceData = m_isServiceThreadFlags.Value;
      if ((serviceData.MaxOperationsAllowed == UNLIMITED_MAX_TASKS) ||
         (serviceData.ExecutedOperationsCount < serviceData.MaxOperationsAllowed))
      {
        return false;
      }

      return true;
    }

    private CancellationToken withWorkCancelToken()
    {
      lock (m_workLockObject)
      {

        return (existsWork()
          ? m_workCancelTokenSource.Token
          : withoutCancelToken());

      }
    }

    private CancellationToken withGlobalCancelToken()
    {
      return m_stopCancelTokenSource.Token;
    }

    private bool existsWork()
    {
      lock (m_workLockObject)
      {
        return (m_workCancelTokenSource != null);
      }
    }

    private CancellationToken withoutCancelToken()
    {
      return CancellationToken.None;
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