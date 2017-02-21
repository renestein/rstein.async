using System.Collections.Generic;

namespace RStein.Async.Examples.MapReduceActors
{
  public interface ICountWordAggregateActor : IActor
  {
    void ProcessNextWordCountDictionary(IReadOnlyDictionary<string, int> wordCounterDictionary);
  }
}