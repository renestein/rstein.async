namespace RStein.Async.Actors.ActorsCore
{
  public class ProxyContext : IProxyContext
  {
    public ProxyContext()
    {
    }

    public static IProxyContext Current
    {
      get;
    } = new ProxyContext();

    public SubjectProxyMapping SubjectProxyMapping
    {
      get;
    } = new SubjectProxyMapping();
  }
}