using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RStein.Async.Tasks
{
  public class DebugTaskCompletionSource<T>
  {
    public const string DEFAULT_BROKEN_PROMISE_DESCRIPTION = "Broken promise DebugTaskCompletionSource - task id {0}";

    private readonly TaskCompletionSource<T> m_taskCompletionSource;
    private readonly string m_description;
    private readonly Task<T> m_task;

    public DebugTaskCompletionSource()
      : this(null)
    {

    }

    public DebugTaskCompletionSource(string description)
    {

      m_taskCompletionSource = new TaskCompletionSource<T>();
      m_task = m_taskCompletionSource.Task;
      m_description = description ?? String.Format(DEFAULT_BROKEN_PROMISE_DESCRIPTION, m_taskCompletionSource.Task.Id);
      registerTask();
    }

#if DEBUG
    ~DebugTaskCompletionSource()
    {
      detectBrokenPromise();

    }
#endif

    public void SetCanceled()
    {
      m_taskCompletionSource.SetCanceled();
    }

    public void SetException(Exception exception)
    {
      m_taskCompletionSource.SetException(exception);
    }

    public void SetException(IEnumerable<Exception> exceptions)
    {
      m_taskCompletionSource.SetException(exceptions);
    }

    public void SetResult(T result)
    {
      m_taskCompletionSource.SetResult(result);
    }

    public Task<T> Task
    {
      get
      {
        return m_taskCompletionSource.Task;
      }
    }
    public bool TrySetCanceled()
    {
      return m_taskCompletionSource.TrySetCanceled();
    }

    public bool TrySetException(Exception exception)
    {
      return m_taskCompletionSource.TrySetException(exception);
    }

    public bool TrySetException(IEnumerable<Exception> exceptions)
    {
      return m_taskCompletionSource.TrySetException(exceptions);
    }

    public bool TrySetResult(T result)
    {
      return m_taskCompletionSource.TrySetResult(result);
    }

    [ConditionalAttribute("DEBUG")]
    private void registerTask()
    {
      TasksFromDebugTaskcompletionSourceHolder.Add(this, m_task);
    }

    [ConditionalAttribute("DEBUG")]
    private void detectBrokenPromise()
    {
      Task task;
      TasksFromDebugTaskcompletionSourceHolder.TryGetValue(this, out task);

      Debug.Assert(task != null);
      if (!task.IsCompleted)
      {
        throw new BrokenPromiseException(m_description);
      }

    }
  }
}