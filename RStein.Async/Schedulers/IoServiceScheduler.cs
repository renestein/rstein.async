using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class IoServiceScheduler : TaskSchedulerBase, IAsioTaskService
  {
    public const int REQUIRED_WORK_CANCEL_TOKEN_VALUE = 1;
    public const int POLL_ONE_RUN_ONE_MAX_TASKS = 1;
    public const int UNLIMITED_MAX_TASKS = -1;

    private readonly ThreadLocal<IoSchedulerThreadServiceFlags> m_isServiceThreadFlags;
    private readonly CancellationTokenSource m_stopCancelTokenSource;
    private readonly BlockingCollection<Task> m_tasks;
    private readonly object m_workLockObject;
    private CancellationTokenSource m_workCancelTokenSource;
    private volatile int m_workCounter;

    public IoServiceScheduler()
    {
      m_tasks = new BlockingCollection<Task>();
      m_isServiceThreadFlags = new ThreadLocal<IoSchedulerThreadServiceFlags>(() => new IoSchedulerThreadServiceFlags());
      m_stopCancelTokenSource = new CancellationTokenSource();
      m_workLockObject = new object();
      m_workCounter = 0;
    }

    public override int MaximumConcurrencyLevel
    {
      get
      {
        CheckIfDisposed();
        return Int32.MaxValue;
      }
    }

    public virtual Task Dispatch(Action action)
    {
      CheckIfDisposed();
      if (action == null)
      {
        throw new ArgumentNullException(nameof(action));
      }

      var task = Task.Factory.StartNew(action,
                                       CancellationToken.None,
                                       TaskCreationOptions.None,
                                       ProxyScheduler.AsTplScheduler());
      return task;
    }

    public virtual Task Dispatch(Func<Task> function)
    {
      CheckIfDisposed();
      if (function == null)
      {
        throw new ArgumentNullException(nameof(function));
      }

      var task = Task.Factory.StartNew(function,
                                       CancellationToken.None,
                                       TaskCreationOptions.None,
                                       ProxyScheduler.AsTplScheduler()).Unwrap();
      return task;
    }

    public virtual Task Post(Action action)
    {
      CheckIfDisposed();

      if (action == null)
      {
        throw new ArgumentNullException(nameof(action));
      }

      return postInner(() => Dispatch(action));
    }

    public virtual Task Post(Func<Task> function)
    {
      CheckIfDisposed();

      if (function == null)
      {
        throw new ArgumentNullException(nameof(function));
      }

      return postInner(() => Dispatch(function));
    }

    public virtual int Run()
    {
      CheckIfDisposed();
      return runTasks(withWorkCancelToken());
    }

    public virtual int RunOne()
    {
      CheckIfDisposed();
      return runTasks(withGlobalCancelToken(), POLL_ONE_RUN_ONE_MAX_TASKS);
    }


    public virtual int Poll()
    {
      CheckIfDisposed();
      return runTasks(withoutCancelToken());
    }

    public virtual int PollOne()
    {
      CheckIfDisposed();
      return runTasks(withoutCancelToken(), maxTasks: POLL_ONE_RUN_ONE_MAX_TASKS);
    }

    public virtual Func<Task> WrapAsTask(Action action)
    {
      CheckIfDisposed();
      if (action == null)
      {
        throw new ArgumentNullException(nameof(action));
      }

      return () => Dispatch(action);
    }

    public virtual Func<Task> WrapAsTask(Func<Task> function)
    {
      CheckIfDisposed();
      if (function == null)
      {
        throw new ArgumentNullException(nameof(function));
      }

      return () => Dispatch(function);
    }

    public virtual Action Wrap(Action action)
    {
      CheckIfDisposed();
      if (action == null)
      {
        throw new ArgumentNullException(nameof(action));
      }

      return () => Dispatch(action);
    }


    public virtual Action Wrap(Func<Task> function)
    {
      CheckIfDisposed();
      if (function == null)
      {
        throw new ArgumentNullException(nameof(function));
      }

      return () => Dispatch(function);
    }

    public override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      CheckIfDisposed();
      if (!isInServiceThread())
      {
        return false;
      }

      if (tasksLimitReached())
      {
        return false;
      }

      var taskExecutedNow = false;
      try
      {
        m_isServiceThreadFlags.Value.ExecutedOperationsCount++;
        taskExecutedNow = task.RunOnProxyScheduler();
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

    public override void QueueTask(Task task)
    {
      CheckIfDisposed();
      m_tasks.Add(task);
    }

    public override IEnumerable<Task> GetScheduledTasks()
    {
      CheckIfDisposed();
      return m_tasks.ToArray();
    }



    internal void AddWork(Work work)
    {
      if (work == null)
      {
        throw new ArgumentNullException(nameof(work));
      }

      handleWorkAdded(work);
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        doStop();
      }
    }

    private Task postInner(Func<Task> dispatcher)
    {
      var oldIsInServiceThread = m_isServiceThreadFlags.Value.IsServiceThread;

      try
      {
        clearCurrentThreadAsServiceFlag();
        return dispatcher();
      }
      finally
      {
        m_isServiceThreadFlags.Value.IsServiceThread = oldIsInServiceThread;
      }
    }


    private bool isInServiceThread()
    {
      return m_isServiceThreadFlags.Value.IsServiceThread;
    }

    private void doStop()
    {
      m_stopCancelTokenSource.Cancel();
      handleWorkDisposed(cancelNow: true);
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

        work.RegisterWorkDisposedHandler(() => handleWorkDisposed());
      }
    }

    private bool isNewWorkCancelTokenRequired()
    {
      return (m_workCounter == REQUIRED_WORK_CANCEL_TOKEN_VALUE);
    }

    private void handleWorkDisposed(bool cancelNow = false)
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
      var searchForTask = true;

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

    private CancellationToken withGlobalCancelToken() => m_stopCancelTokenSource.Token;

    private bool existsWork()
    {
      lock (m_workLockObject)
      {
        return (m_workCancelTokenSource != null);
      }
    }

    private CancellationToken withoutCancelToken() => CancellationToken.None;
  }
}