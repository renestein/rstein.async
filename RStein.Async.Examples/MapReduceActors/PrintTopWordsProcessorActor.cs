using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core;
using RStein.Async.Examples.Extensions;

namespace RStein.Async.Examples.MapReduceActors
{
  public class PrintTopWordsProcessorActor : ActorBase, IResultProcessorActor
  {
    public const int TOP_WORDS_COUNT = 100;
    public const string TOP_WORD_MESSAGE_FORMAT = "{0, -10} : {1}";
    public const string TOTAL_DOSTINCT_WORD_MESSAGE_FORMAT = "Total distinct words : {0}";
    public const string TOTAL_WORD_MESSAGE_FORMAT = "Total_words : {0}";

    public PrintTopWordsProcessorActor() : base() {}

    public virtual void ProcessFinalWordCountDictionary(IReadOnlyDictionary<string, int> wordCounterDictionary)
    {
      var allWordsSum = wordCounterDictionary.Sum(pair => pair.Value);
      Console.WriteLine(TOTAL_WORD_MESSAGE_FORMAT, allWordsSum);
      Console.WriteLine(TOTAL_DOSTINCT_WORD_MESSAGE_FORMAT, wordCounterDictionary.Count);
      wordCounterDictionary.OrderByDescending(pair => pair.Value)
        .Take(TOP_WORDS_COUNT).ForEach(pair => Console.WriteLine(TOP_WORD_MESSAGE_FORMAT, pair.Key.ToLower(), pair.Value));
    }
  }
}