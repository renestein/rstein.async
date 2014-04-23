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
    public const int POLL_ONE_MAX_TASKS = 1;
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


    public virtual void Dispatch(Action action)
    {
      checkIfDisposed();
      if (action == null)
      {
        throw new ArgumentNullException("action");
      }

      var task = new Task(action);
      TryExecuteTaskInline(task, taskWasPreviouslyQueued: false);
    }


    public virtual int Poll()
    {
      checkIfDisposed();
      return runTasks();
    }

    public virtual int PollOne()
    {
      checkIfDisposed();
      return runTasks(maxTasks: POLL_ONE_MAX_TASKS);
    }

    public virtual void Post(Action action)
    {
      if (action == null)
      {
        throw new ArgumentNullException("action");
      }
      var newTask = new Task(action);
      QueueTask(newTask);
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

    private bool isInServiceThread()
    {
      return m_isServiceThreadMark.Value;
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

    private int runTasks(bool waitForWorkCancel = false, int maxTasks = UNLIMITED_MAX_TASKS)
    {
      try
      {
        markCurrentThreadAsServiceThread();
        return runTasksCore(waitForWorkCancel, maxTasks);
      }
      finally
      {
        clearCurrentThreadAsServiceMark();
      }
    }

    private int runTasksCore(bool waitForWorkCancel, int maxTasks)
    {
      var usedCancelToken = waitForWorkCancel
        ? CancellationTokenSource.CreateLinkedTokenSource(m_stopCancelTokenSource.Token, m_workCancelTokenSource.Token).Token
        : m_stopCancelTokenSource.Token;

      int tasksExecuted = 0;
      try
      {
        Task task;
        while (!taskLimitReached(tasksExecuted, maxTasks) &&
              m_tasks.TryTake(out task, Timeout.Infinite, usedCancelToken))
        {
          tasksExecuted++;
          TryExecuteTask(task);
        }
      }
      catch (OperationCanceledException e)
      {
        Trace.WriteLine(e);
      }
      return tasksExecuted;
    }

    private bool taskLimitReached(int tasksExecuted, int maxTasks)
    {
      if ((maxTasks == UNLIMITED_MAX_TASKS) ||
         (tasksExecuted < maxTasks))
      {
        return false;
      }

      return true;
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