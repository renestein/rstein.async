using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RStein.Async.Examples.MapReduceActors
{
  public interface ILibraryActor : IActor
  {
    void AddBook(string title);
    Task<IEnumerable<string>> GetBooks();
    Task<string> GetLastBook();
  }
}