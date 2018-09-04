using System.Threading;
using static System.Threading.Interlocked;

namespace RStein.Async.Threading
{
  public class LeftRightVersion
  {
    private const int DUMMY_VALUE = int.MinValue;
    public const int RESET_EVENT_VALUE = 1;
    public const int SET_EVENT_VALUE = 0;
    private int m_counter;
    private ManualResetEventSlim m_waitEvent;

    public LeftRightVersion()
    {
      m_counter = 0;
      m_waitEvent= new ManualResetEventSlim(initialState:true);
    }

    public void Arrive()
    {
      var newValue  = Increment(ref m_counter);
      if (newValue == RESET_EVENT_VALUE)
      {
        m_waitEvent.Reset();
      }
    }

    public void Depart()
    {
      var newValue = Decrement(ref m_counter);
      if (newValue == SET_EVENT_VALUE)
      {
        m_waitEvent.Set();
      }
    }

    public bool IsEmpty() => CompareExchange(ref m_counter, DUMMY_VALUE, DUMMY_VALUE) == 0;

    public void WaitForEmptyVersion()
    {
      m_waitEvent.Wait();
    }
  }
}