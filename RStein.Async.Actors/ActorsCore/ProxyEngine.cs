using System;
using Castle.DynamicProxy;
using RStein.Async.Schedulers;

namespace RStein.Async.Actors.ActorsCore
{
  public class ProxyEngine
  {
    private readonly ITaskScheduler m_primaryScheduler;
    private readonly ProxyGenerator m_proxyGenerator;
    private readonly ProxyGenerationOptions m_proxyOptions;

    public ProxyEngine(ITaskScheduler primaryScheduler)
    {
      m_primaryScheduler = primaryScheduler ?? throw new ArgumentNullException(nameof(primaryScheduler));


      m_proxyGenerator = new ProxyGenerator();
      m_proxyOptions = new ProxyGenerationOptions(new ProxyGenerationHook());
    }

    public virtual TActorInterface CreateProxy<TActorInterface>(TActorInterface targetObject)
      where TActorInterface : class
    {
      if (targetObject == null)
      {
        throw new ArgumentNullException(nameof(targetObject));
      }

      var retProxy = m_proxyGenerator.CreateInterfaceProxyWithTargetInterface(typeof (TActorInterface),
        targetObject,
        m_proxyOptions,
        new PreventArgumentBaseTypeLeakInterceptor(),
        new ActorMethodInterceptor(m_primaryScheduler));

      ProxyContext.Current
        .SubjectProxyMapping
        .AddSubjectProxyPair(targetObject, retProxy);

      return retProxy as TActorInterface;
    }
  }
}