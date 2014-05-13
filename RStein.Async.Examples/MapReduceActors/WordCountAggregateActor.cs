using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Castle.Core;

namespace RStein.Async.Examples.MapReduceActors
{
  public class WordCountAggregateActor : ActorBase, ICountWordAggregateActor
  {

    private readonly IResultProcessorActor m_resultProcessor;
    private readonly Dictionary<string, int> m_wordCountDictionary;

    public WordCountAggregateActor(IResultProcessorActor resultProcessor, int completeCountDownCount)
      : base(completeCountDownCount)
    {
      if (resultProcessor == null)
      {
        throw new ArgumentNullException("resultProcessor");
      }

      m_resultProcessor = resultProcessor;
      m_wordCountDictionary = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
    }


    public virtual void ProcessNextWordCountDictionary(IReadOnlyDictionary<string, int> nextWordCounter)
    {
      nextWordCounter.ForEach(accumulateNextPair);
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