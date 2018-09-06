using System;
using RStein.Async.Actors.ActorsCore;


namespace RStein.Async.Examples.MapReduceActors
{
  public class BookLineConsumerFactory : IBookLineConsumerFactory
  {
    private readonly ICountWordAggregateActor m_aggregateActor;
    private readonly ProxyEngine m_proxyEngine;

    public BookLineConsumerFactory(ICountWordAggregateActor aggregateActor, ProxyEngine proxyEngine)
    {
      m_aggregateActor = aggregateActor ?? throw new ArgumentNullException(nameof(aggregateActor));
      m_proxyEngine = proxyEngine ?? throw new ArgumentNullException(nameof(proxyEngine));
    }

    public virtual IBookLineConsumerActor CreateConsumer(int consumerId)
    {
      IBookLineConsumerActor consumer = new CountWordsInLineActor(consumerId, m_aggregateActor);
      consumer = m_proxyEngine.CreateProxy(consumer);
      return consumer;
    }
  }
}