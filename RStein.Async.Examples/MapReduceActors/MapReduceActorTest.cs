using System.Threading.Tasks;
using RStein.Async.Actors.ActorsCore;
using RStein.Async.Schedulers;

namespace RStein.Async.Examples.MapReduceActors
{
  public class MapReduceActorTest
  {
    public const string BOOK_NAME = "shakespeare.txt";

    public async Task Run()
    {
      var ioService = new IoServiceScheduler();
      var threadPool = new IoServiceThreadPoolScheduler(ioService);
      var proxyEngine = new ProxyEngine(threadPool);

      ILibraryActor library = new LibraryActor();
      library = proxyEngine.CreateProxy(library);

      IResultProcessorActor resultProcessorActor = new PrintTopWordsProcessorActor();
      resultProcessorActor = proxyEngine.CreateProxy(resultProcessorActor);

      ICountWordAggregateActor aggregateWordCountActor = new WordCountAggregateActor(resultProcessorActor, BookLinesParserActor.NUMBER_OF_CONSUMERS);
      aggregateWordCountActor = proxyEngine.CreateProxy(aggregateWordCountActor);

      IBookLinesParserActor linesParserActor = new BookLinesParserActor(library, new BookLineConsumerFactory(aggregateWordCountActor, proxyEngine));
      linesParserActor = proxyEngine.CreateProxy(linesParserActor);

      library.AddBook(BOOK_NAME);

      await linesParserActor.ProcessLastBook().ConfigureAwait(false);


      linesParserActor.Complete();

      await resultProcessorActor.Completed;
    }
  }
}