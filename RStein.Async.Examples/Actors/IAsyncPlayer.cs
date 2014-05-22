using System.Threading.Tasks;

namespace RStein.Async.Examples.Actors
{
  public interface IAsyncPlayer
  {
    Task Ping(int pingCount, IAsyncPlayer secondPlayer, string gameName);
  }
}