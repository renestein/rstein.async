using System;
using System.Threading;
using System.Threading.Tasks;
using RStein.Async.Tasks;

namespace RStein.Async.Schedulers
{
  public class IoServiceSynchronizationContext : SynchronizationContext
  {
    private readonly bool m_disposeIoServiceAfterComplete;
    private const string SEND_FAILED_EXCEPTION_MESSAGE = "Send method failed";
    private readonly IoServiceScheduler m_ioServiceScheduler;
    private int m_outstandingOperationCount;
    private Work m_work;

    public IoServiceSynchronizationContext(IoServiceScheduler ioServiceScheduler)
      : this(ioServiceScheduler, disposeIoServiceAfterComplete: false)
    {

    }

    public IoServiceSynchronizationContext(IoServiceScheduler ioServiceScheduler, bool disposeIoServiceAfterComplete)
    {
      if (ioServiceScheduler == null)
      {
        throw new ArgumentNullException("ioServiceScheduler");
      }

      m_disposeIoServiceAfterComplete = disposeIoServiceAfterComplete;
      m_ioServiceScheduler = ioServiceScheduler;
      m_outstandingOperationCount = 0;
      m_work = new Work(ioServiceScheduler);
      
    }

    public override void Post(SendOrPostCallback d, object state)
    {
      m_ioServiceScheduler.Post(() => d(state));

    }

    public override void Send(SendOrPostCallback d, object state)
    {
      Task sendTask = m_ioServiceScheduler.Dispatch(() => d(state));
      sendTask.WaitAndPropagateException();
      
    }

    public override void OperationStarted()
    {
      Interlocked.Increment(ref m_outstandingOperationCount);
    }

    public override void OperationCompleted()
    {
      int oustandingOperations = Interlocked.Decrement(ref m_outstandingOperationCount);
      tryCompleteContext(oustandingOperations);

    }

    public override SynchronizationContext CreateCopy()
    {
      return this;
    }

    private void tryCompleteContext(int outstandingOperations)
    {
      if (outstandingOperations == 0 && m_disposeIoServiceAfterComplete)
      {
        m_work.Dispose();
        m_ioServiceScheduler.Dispose();
      }
    }
  }
}