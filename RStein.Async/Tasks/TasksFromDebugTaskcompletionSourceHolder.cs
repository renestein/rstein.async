using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RStein.Async.Tasks
{
  public static class TasksFromDebugTaskcompletionSourceHolder
  {
    private static ConditionalWeakTable<object, Task> _tasksDictionary = new ConditionalWeakTable<object, Task>();
    public static void Add(object key, Task value)
    {
      _tasksDictionary.Add(key, value);
    }

    public static void Remove(object key)
    {
      _tasksDictionary.Remove(key);
    }

    public static bool TryGetValue(object key, out Task value)
    {
      return _tasksDictionary.TryGetValue(key, out value);
    }
  }
}