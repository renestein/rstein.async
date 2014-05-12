using System;
using Castle.DynamicProxy;
using RStein.Async.Schedulers;

namespace RStein.Async.Examples.ActorsCore
{
  public class ProxyEngine
  {
    private readonly ITaskScheduler m_primaryScheduler;
    private readonly ProxyGenerator m_proxyGenerator = new ProxyGenerator();
    private readonly ProxyGenerationOptions m_proxyOptions = new ProxyGenerationOptions();

    public ProxyEngine(ITaskScheduler primaryScheduler)
    {
      if (primaryScheduler == null)
      {
        throw new ArgumentNullException("primaryScheduler");
      }


      m_primaryScheduler = primaryScheduler;


      m_proxyGenerator = new ProxyGenerator();
      m_proxyOptions = new ProxyGenerationOptions(new ProxyGenerationHook());
    }

    public virtual TActorInterface CreateProxy<TActorInterface>(TActorInterface targetObject)
      where TActorInterface : class
    {

      if (targetObject == null)
      {
        throw new ArgumentNullException("targetObject");
      }

      var retProxy = m_proxyGenerator.CreateInterfaceProxyWithTargetInterface(typeof(TActorInterface),
                                                                              targetObject,
                                                                              m_proxyOptions,
                                                                              new ActorMethodInterceptor(m_primaryScheduler),
                                                                              new PreventArgumentBaseTypeLeakInterceptor());
      ProxyContext.Current
                  .SubjectProxyMapping
                  .AddSubjectProxyPair(targetObject, retProxy);

      return retProxy as TActorInterface;
    }
  }
}
