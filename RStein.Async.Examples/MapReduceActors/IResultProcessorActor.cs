using System.Collections.Generic;

namespace RStein.Async.Examples.MapReduceActors
{
  public interface IResultProcessorActor : IActor
  {
    void ProcessFinalWordCountDictionary(IReadOnlyDictionary<string, int> wordCounterDictionary);
  }
}