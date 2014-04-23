using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RStein.Async.Schedulers
{
  public class IoServiceScheduler : TaskScheduler
  {
    private BlockingCollection<Task> m_tasks;
    private volatile int m_workCounter;
    private object m_workLockObject;
    private ThreadLocal<bool> m_isServiceThreadHolder;
    private CancellationTokenSource m_stopCancelTokenSource;
    private CancellationTokenSource m_workCancelTokensource;

    public IoServiceScheduler()
    {
      m_tasks = new BlockingCollection<Task>();
      m_stopCancelTokenSource = new CancellationTokenSource();
      m_workCancelTokensource = new CancellationTokenSource();
      m_workLockObject = new object();
      m_workCounter = 0;
    }

    public virtual void Run()
    {
      try
      {
        markCurrentThreadAsServiceThread();
        runAllTasks(isWorkPresent());
      }
      finally
      {
        clearCurrentThreadServiceMark();
      }
    }

    private void clearCurrentThreadServiceMark()
    {
      m_isServiceThreadHolder.Value = false;
    }

    private void markCurrentThreadAsServiceThread()
    {
      m_isServiceThreadHolder.Value = true;
    }


    public virtual void Stop()
    {
    }

    internal void AddWork(Work work)
    {
      if (work == null)
      {
        throw new ArgumentNullException("work");
      }

      handleWorkAdded(work);
    }


    private void handleWorkAdded(Work work)
    {
      lock (m_workLockObject)
      {
        m_workCounter++;
        work.CancelToken.Register(handleWorkCanceled);
      }
    }

    private void handleWorkCanceled()
    {
      lock (m_workLockObject)
      {
        var cancellationTokenSource = m_workCancelTokensource;

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
      m_isServiceThreadHolder = new ThreadLocal<bool>();
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
      if (!m_isServiceThreadHolder.Value)
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
        ? CancellationTokenSource.CreateLinkedTokenSource(m_stopCancelTokenSource.Token, m_workCancelTokensource.Token).Token
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
  }
}