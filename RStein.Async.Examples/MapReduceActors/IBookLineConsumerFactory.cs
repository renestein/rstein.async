namespace RStein.Async.Examples.MapReduceActors
{
  public interface IBookLineConsumerFactory
  {
    IBookLineConsumerActor CreateConsumer(int consumerId);
  }
}