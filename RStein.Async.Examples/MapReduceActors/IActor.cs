using System.Threading.Tasks;

namespace RStein.Async.Examples.MapReduceActors
{
  public interface IActor
  {
    Task Completed
    {
      get;
    }
    void Complete();
  }
}