using System.Threading.Tasks;

namespace RStein.Async.Examples.MapReduceActors
{
  public interface IBookLinesParserActor : IActor
  {
    Task ProcessLastBook();
  }
}