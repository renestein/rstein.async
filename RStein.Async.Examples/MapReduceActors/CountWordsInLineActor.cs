﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace RStein.Async.Examples.MapReduceActors
{
  public class CountWordsInLineActor : ActorBase, IBookLineConsumerActor
  {
    private const string SPLIT_WORDS_REGEX = @"\W+(?<!')";
    private const int FIRST_WORD_OCCURENCE = 1;
    private readonly ICountWordAggregateActor m_countWordAggregateActor;
    private readonly int m_id;

    public CountWordsInLineActor(int id, ICountWordAggregateActor countWordAggregateActor)
    {
      if (countWordAggregateActor == null)
      {
        throw new ArgumentNullException("countWordAggregateActor");
      }

      m_id = id;
      m_countWordAggregateActor = countWordAggregateActor;
    }

    public virtual void AddBookLine(string line)
    {
      IEnumerable<String> words = Regex.Split(line, SPLIT_WORDS_REGEX);
      words = words.Where(word => !String.IsNullOrWhiteSpace(word));
      var wordCountDictionary = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

      foreach (var word in words)
      {
        int currentValue;
        if (wordCountDictionary.TryGetValue(word, out currentValue))
        {
          wordCountDictionary[word] = ++currentValue;
        }
        else
        {
          wordCountDictionary.Add(word, FIRST_WORD_OCCURENCE);
        }
      }

      m_countWordAggregateActor.ProcessNextWordCountDictionary(new ReadOnlyDictionary<string, int>(wordCountDictionary));
    }

    protected override void DoInnerComplete()
    {
      m_countWordAggregateActor.Complete();
    }
  }
}