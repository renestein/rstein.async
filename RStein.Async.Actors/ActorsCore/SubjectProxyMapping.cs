using System;
using System.Runtime.CompilerServices;

namespace RStein.Async.Actors.ActorsCore
{
  public class SubjectProxyMapping
  {
    public ConditionalWeakTable<Object, Object> m_baseProxyMapping;

    public SubjectProxyMapping()
    {
      m_baseProxyMapping = new ConditionalWeakTable<object, object>();
    }

    public void AddSubjectProxyPair(object realObject, object proxy)
    {
      if (realObject == null)
      {
        throw new ArgumentNullException(nameof(realObject));
      }
      if (proxy == null)
      {
        throw new ArgumentNullException(nameof(proxy));
      }

      m_baseProxyMapping.Add(realObject, proxy);
    }

    public Object TryFindProxy(Object realObject)
    {
      if (realObject == null)
      {
        throw new ArgumentNullException(nameof(realObject));
      }

      object proxy;
      m_baseProxyMapping.TryGetValue(realObject, out proxy);

      return proxy;
    }
  }
}