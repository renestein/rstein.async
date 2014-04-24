using System.Xml;

namespace RStein.Async.Schedulers
{
  public class IoSchedulerThreadServiceFlags
  {
    public IoSchedulerThreadServiceFlags()
    {
      ResetData();
    }
    public bool IsServiceThread
    {
      get;
      set;
    }

    public int MaxOperationsAllowed
    {
      get;
      set;
    }

    public int ExecutedOperationsCount
    {
      get;
      set;
    }

    public void ResetData()
    {
      IsServiceThread = false;
      MaxOperationsAllowed = ExecutedOperationsCount = 0;
    }
  }
}