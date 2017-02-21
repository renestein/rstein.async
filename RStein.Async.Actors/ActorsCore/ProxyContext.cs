namespace RStein.Async.Actors.ActorsCore
{
  //TODO: Refactor ProxyContext
  public class ProxyContext : IProxyContext
  {
    private static IProxyContext _currentContext = new ProxyContext();
    private readonly SubjectProxyMapping m_subjectProxyMapping;

    public ProxyContext()
    {
      m_subjectProxyMapping = new SubjectProxyMapping();
    }

    public static IProxyContext Current
    {
      get
      {
        return _currentContext;
      }
    }

    public SubjectProxyMapping SubjectProxyMapping
    {
      get
      {
        return m_subjectProxyMapping;
      }
    }
  }
}