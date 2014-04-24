using System;
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
    private readonly ThreadLocal<bool> m_isServiceThreadFlag;
    private readonly CancellationTokenSource m_stopCancelTokenSource;
    private CancellationTokenSource m_workCancelTokenSource;
    private volatile bool m_isDisposed;

    public IoServiceScheduler()
    {
      m_tasks = new BlockingCollection<Task>();
      m_isServiceThreadFlag = new ThreadLocal<bool>();
      m_stopCancelTokenSource = new CancellationTokenSource();
      m_workLockObject = new object();
      m_serviceLockObject = new object();
      m_workCounter = 0;
    }

    public virtual int Run()
    {
      checkIfDisposed();
      return runTasks(withWorkkCancelToken());
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

      var task = new Task(action);
      task.Start(this);
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

      bool oldIsInServiceThread = m_isServiceThreadFlag.Value;

      try
      {
        clearCurrentThreadAsServiceFlag();
        return Dispatch(action);
      }
      finally
      {
        m_isServiceThreadFlag.Value = oldIsInServiceThread;
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

    private bool isInServiceThread()
    {
      return m_isServiceThreadFlag.Value;
    }

    private void doStop()
    {
      m_stopCancelTokenSource.Cancel();
      handleWorkCanceled(cancelNow: true);
    }

    private void clearCurrentThreadAsServiceFlag()
    {
      m_isServiceThreadFlag.Value = false;
    }

    private void markCurrentThreadAsServiceThread()
    {
      m_isServiceThreadFlag.Value = true;
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

    private int runTasks(CancellationToken cancellationToken, int maxTasks = UNLIMITED_MAX_TASKS)
    {
      try
      {
        markCurrentThreadAsServiceThread();
        return runTasksCore(cancellationToken, maxTasks);
      }
      finally
      {
        clearCurrentThreadAsServiceFlag();
      }
    }

    private int runTasksCore(CancellationToken cancellationToken, int maxTasks)
    {

      int tasksExecuted = 0;
      bool searchForTask = true;

      var usedCancellationToken = cancellationToken;

      while (!tasksLimitReached(tasksExecuted, maxTasks) && searchForTask)
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

          searchForTask = true;
          m_stopCancelTokenSource.Token.ThrowIfCancellationRequested();

          TryExecuteTaskInline(task, true);
          tasksExecuted++;

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

    private CancellationToken withWorkkCancelToken()
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