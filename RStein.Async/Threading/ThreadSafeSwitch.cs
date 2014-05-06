using System.Threading;

namespace RStein.Async.Threading
{
  public class ThreadSafeSwitch
  {
    private const int SWITCH_ON = 1;
    private const int SWITCH_OFF = 0;
    private const int INVALID_VALUE = -1;
    private int m_safeSwitch;

    public ThreadSafeSwitch()
    {
      m_safeSwitch = SWITCH_OFF;
    }

    public bool TrySet()
    {
      int oldValue = Interlocked.CompareExchange(ref m_safeSwitch, SWITCH_ON, SWITCH_OFF);
      return (oldValue == SWITCH_OFF);
    }

    public bool TryReset()
    {
      int oldValue = Interlocked.CompareExchange(ref m_safeSwitch, SWITCH_OFF, SWITCH_ON);
      return (oldValue == SWITCH_ON);
    }

    public bool Value
    {
      get
      {
        int oldValue = Interlocked.CompareExchange(ref m_safeSwitch, INVALID_VALUE, INVALID_VALUE);
        return (oldValue == SWITCH_ON);
      }
    }
  }
}