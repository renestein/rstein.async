﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class IoServiceScheduler : TaskScheduler, IDisposable
  {
    public const int REQUIRED_WORK_CANCEL_TOKEN_VALUE = 1;
    public const int POLLONE_RUNONE_MAX_TASKS = 1;
    public const int UNLIMITED_MAX_TASKS = -1;
    private readonly BlockingCollection<Task> m_tasks;
    private volatile int m_workCounter;
    private readonly object m_workLockObject;
    private readonly object m_serviceLockObject;
    private readonly ThreadLocal<bool> m_isServiceThreadMark;
    private readonly CancellationTokenSource m_stopCancelTokenSource;
    private CancellationTokenSource m_workCancelTokenSource;
    private volatile bool m_isDisposed;

    public IoServiceScheduler()
    {
      m_tasks = new BlockingCollection<Task>();
      m_isServiceThreadMark = new ThreadLocal<bool>();
      m_stopCancelTokenSource = new CancellationTokenSource();
      m_workLockObject = new object();
      m_serviceLockObject = new object();
      m_workCounter = 0;
    }

    public virtual int Run()
    {
      checkIfDisposed();
      return runTasks(isWorkPresent());
    }

    public virtual int RunOne()
    {
      checkIfDisposed();
      return runTasks(isWorkPresent(), POLLONE_RUNONE_MAX_TASKS);

    }

    public virtual Task Dispatch(Action action)
    {
      checkIfDisposed();
      if (action == null)
      {
        throw new ArgumentNullException("action");
      }

      var task = new Task(action);
      task.Start(this);
      return task;
    }

    public virtual int Poll()
    {
      checkIfDisposed();
      return runTasks(ignoreWork());
    }

    public virtual int PollOne()
    {
      checkIfDisposed();
      return runTasks(ignoreWork(), maxTasks: POLLONE_RUNONE_MAX_TASKS);
    }

    public virtual Task Post(Action action)
    {
      checkIfDisposed();

      if (action == null)
      {
        throw new ArgumentNullException("action");
      }

      bool oldIsInServiceThread = m_isServiceThreadMark.Value;
      
      try
      {
        m_isServiceThreadMark.Value = false;
        return Dispatch(action);
      }
      finally
      {
        m_isServiceThreadMark.Value = oldIsInServiceThread;
      }
      
    }

    public virtual Action Wrap(Action action)
    {
      if (action == null)
      {
        throw new ArgumentNullException("action");
      }

      return () => Dispatch(action);
    }

    public virtual Func<Task> WrapAsTask(Action action)
    {
      if (action == null)
      {
        throw new ArgumentNullException("action");
      }

      return () => Dispatch(action);
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

    private bool isInServiceThread()
    {
      return m_isServiceThreadMark.Value;
    }

    private void doStop()
    {
      m_stopCancelTokenSource.Cancel();
    }

    private void clearCurrentThreadAsServiceFlag()
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

    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      if (!isInServiceThread())
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

    private int runTasks(Tuple<bool, CancellationTokenSource> waitForWorkCancel, int maxTasks = UNLIMITED_MAX_TASKS)
    {
      try
      {
        markCurrentThreadAsServiceThread();
        return runTasksCore(waitForWorkCancel, maxTasks);
      }
      finally
      {
        clearCurrentThreadAsServiceFlag();
      }
    }

    private int runTasksCore(Tuple<bool, CancellationTokenSource> waitForWorkCancel, int maxTasks)
    {

      var shouldUseWorkCancelToken = waitForWorkCancel.Item1;
      var currentWorkCancelTokenSource = shouldUseWorkCancelToken ? waitForWorkCancel.Item2 : null;

      var usedCancelToken = shouldUseWorkCancelToken
        ? CancellationTokenSource.CreateLinkedTokenSource(m_stopCancelTokenSource.Token, currentWorkCancelTokenSource.Token).Token
        : CancellationToken.None;

      int tasksExecuted = 0;
      bool searchForTask = true;

      while (!tasksLimitReached(tasksExecuted, maxTasks) && searchForTask)
      {
        searchForTask = false;

        try
        {
          Task task;
          while (tryGetTask(usedCancelToken,  out task))
          {
            searchForTask = true;
            tasksExecuted++;
            TryExecuteTaskInline(task, true);
            m_stopCancelTokenSource.Token.ThrowIfCancellationRequested();
          }
        }

        catch (OperationCanceledException e)
        {
          Trace.WriteLine(e);

          if (m_stopCancelTokenSource.IsCancellationRequested)
          {
            break;
          }

          usedCancelToken = CancellationToken.None;
          searchForTask = true;
        }
      }


      return tasksExecuted;
    }

    private bool tryGetTask(CancellationToken cancellationToken, out Task task)
    {
      if (cancellationToken != CancellationToken.None)
      {
        return m_tasks.TryTake(out task, Timeout.Infinite, cancellationToken);
      }

      return m_tasks.TryTake(out task);
    }

    private bool tasksLimitReached(int tasksExecuted, int maxTasks)
    {
      if ((maxTasks == UNLIMITED_MAX_TASKS) ||
         (tasksExecuted < maxTasks))
      {
        return false;
      }

      return true;
    }

    private Tuple<bool, CancellationTokenSource> isWorkPresent()
    {
      lock (m_workLockObject)
      {
        var isWorkPresent = m_workCancelTokenSource != null;
        return Tuple.Create(isWorkPresent, m_workCancelTokenSource);
      }
    }

    private Tuple<bool, CancellationTokenSource> ignoreWork()
    {
      return Tuple.Create(false, (CancellationTokenSource)null);
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