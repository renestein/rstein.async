using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RStein.Async.Examples.Extensions;

namespace RStein.Async.Examples.MapReduceActors
{
  public class WordCountAggregateActor : ActorBase, ICountWordAggregateActor
  {
    private readonly IResultProcessorActor m_resultProcessor;
    private readonly Dictionary<string, int> m_wordCountDictionary;

    public WordCountAggregateActor(IResultProcessorActor resultProcessor, int completeCountDownCount)
      : base(completeCountDownCount)
    {
      m_resultProcessor = resultProcessor ?? throw new ArgumentNullException(nameof(resultProcessor));
      m_wordCountDictionary = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
    }


    public virtual void ProcessNextWordCountDictionary(IReadOnlyDictionary<string, int> nextWordCounter)
    {
      foreach (var keyValuePair in nextWordCounter)
      {
        accumulateNextPair(keyValuePair);
      }
    }

    protected override void DoInnerComplete()
    {
      m_resultProcessor.ProcessFinalWordCountDictionary(new ReadOnlyDictionary<string, int>(m_wordCountDictionary));
      m_resultProcessor.Complete();
    }

    private void accumulateNextPair(KeyValuePair<string, int> pair)
    {
      if (m_wordCountDictionary.ContainsKey(pair.Key))
      {
        m_wordCountDictionary[pair.Key] = m_wordCountDictionary[pair.Key] + pair.Value;
      }
      else
      {
        m_wordCountDictionary.Add(pair.Key, pair.Value);
      }
    }
  }
}