using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RStein.Async.Examples.Extensions
{
  public static class LinqExtensions
  {
    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
      if (source == null)
      {
        throw new ArgumentNullException(nameof(source));
      }

      if (action == null)
      {
        throw new ArgumentNullException(nameof(action));
      }

      return ForEachInner();

      IEnumerable<T> ForEachInner()
      {
        foreach (var src in source)
        {
          action(src);
          
        }
        return source;
      }
    }
  }
}