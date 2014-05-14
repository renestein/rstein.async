using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.Core;

namespace RStein.Async.Examples.MapReduceActors
{
  public class BookLinesParserActor : ActorBase, IBookLinesParserActor
  {
    private const string FILE_ENCODING = "windows-1250";
    public static readonly int NUMBER_OF_CONSUMERS = Environment.ProcessorCount;
    private readonly ILibraryActor m_library;
    private readonly IBookLineConsumerFactory m_lineConcumerFactory;
    private IBookLineConsumerActor[] m_consumers;

    public BookLinesParserActor(ILibraryActor library, IBookLineConsumerFactory lineConcumerFactory)
    {
      if (library == null)
      {
        throw new ArgumentNullException("library");
      }

      if (lineConcumerFactory == null)
      {
        throw new ArgumentNullException("lineConcumerFactory");
      }

      m_library = library;
      m_lineConcumerFactory = lineConcumerFactory;
      createBookLineConsumers();
    }

    private void createBookLineConsumers()
    {      
      m_consumers = Enumerable.Range(0, NUMBER_OF_CONSUMERS)
                               .Select(index => m_lineConcumerFactory.CreateConsumer(index))
                               .ToArray();
    }

    protected override void DoInnerComplete()
    {
      m_consumers.ForEach(actor => actor.Complete());
    }

    public virtual async Task ProcessLastBook()
    {
      var lastBook = await m_library.GetLastBook();
      parseLines(lastBook);
    }

    private void parseLines(string lastBookName)
    {
      var lines = File.ReadLines(lastBookName, Encoding.GetEncoding(FILE_ENCODING));
      var consumers = iterateConsumers();
      var lineActorPairs = lines.Zip(consumers, (line, actor) => new
                                            {
                                              Line = line,
                                              Actor = actor
                                            });

      foreach (var lineActorPair in lineActorPairs)
      {
        lineActorPair.Actor.AddBookLine(lineActorPair.Line);
      }
    }

    private IEnumerable<IBookLineConsumerActor> iterateConsumers()
    {
      int consumersCount = m_consumers.Length;
      int currentIndex = 0;
      while (true)
      {
        yield return m_consumers[currentIndex++ % consumersCount];
      }
    }
  }
}