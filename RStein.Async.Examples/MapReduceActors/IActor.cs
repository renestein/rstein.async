using System.Threading.Tasks;

namespace RStein.Async.Examples.MapReduceActors
{
  public interface IActor
  {
    void Complete();
    Task Completed
    {
      get;
    }
  }
}