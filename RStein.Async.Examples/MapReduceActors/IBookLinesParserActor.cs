using System.Collections;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace RStein.Async.Examples.MapReduceActors
{
  public interface IBookLinesParserActor : IActor
  {
    Task ProcessLastBook();
  }
}