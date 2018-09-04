using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RStein.Async.Threading;

namespace RStein.Async.Examples.LeftRight
{
  public class LeftRightList
  {
    private LeftRight<List<int>> m_leftRight;
    const int readers = 10;
    const int writers = 5;

    public LeftRightList()
    {
      m_leftRight = new LeftRight<List<int>>(() => new List<int>());
    }


    public void Execute(CancellationToken cancelToken)
    {
      var readerTasks = Enumerable.Range(0, readers).Select(index => Task.Run(() =>
                                                                              {
                                                                                while (!cancelToken.IsCancellationRequested)
                                                                                {
                                                                                  var currentCount = m_leftRight.Read(list => list.Count);
                                                                                  Console.WriteLine($"Reader: {index}: {currentCount}");
                                                                                  
                                                                                }
                                                                              })).ToArray();

      var writerTasks = Enumerable.Range(0, writers).Select(index => Task.Run(() =>
                                                                                 {
                                                                                   m_leftRight.Write(list =>
                                                                                                     {
                                                                                                        list.Add(index);
                                                                                                        return 0;
                                                                                                     });

                                                                                   Console.WriteLine($"*******Writer: {index}:");

                                                                                 })).ToArray();
      Task.WaitAll(readerTasks.Union(writerTasks).ToArray());

    }
  }
}