namespace RStein.Async.Examples.Actors
{
  public interface IPlayer
  {
    void Ping(int pingCount, IPlayer secondPlayer);
  }
}