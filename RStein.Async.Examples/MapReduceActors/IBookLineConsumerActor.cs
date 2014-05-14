namespace RStein.Async.Examples.MapReduceActors
{
  public interface IBookLineConsumerActor : IActor
  {
    void AddBookLine(string line);
  }
}