using System.Collections.Generic;
using System.Threading.Tasks;

namespace RStein.Async.Examples.MapReduceActors
{
  public class LibraryActor : ActorBase, ILibraryActor
  {
    private List<string> m_books;
    private string m_lastBook;
    public LibraryActor()
    {
      m_books = new List<string>();
    }

    public virtual void AddBook(string title)
    {
      m_books.Add(title);
      m_lastBook = title;
    }

    public Task<IEnumerable<string>> GetBooks()
    {
      return Task.FromResult(m_books.ToArray() as IEnumerable<string>);
    }

    public Task<string> GetLastBook()
    {
      return Task.FromResult(m_lastBook);
    }
  }
}