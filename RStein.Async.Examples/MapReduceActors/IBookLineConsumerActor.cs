using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace RStein.Async.Examples.MapReduceActors
{
  public interface IBookLineConsumerActor : IActor
  {
    void AddBookLine(string line);
  }
}