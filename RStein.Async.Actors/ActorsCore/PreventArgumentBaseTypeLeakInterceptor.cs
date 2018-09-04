using Castle.DynamicProxy;

namespace RStein.Async.Actors.ActorsCore
{
  public class PreventArgumentBaseTypeLeakInterceptor : IInterceptor
  {
    public void Intercept(IInvocation invocation)
    {
      preventProxySubjectLeakForAllArgs(invocation);
      invocation.Proceed();
    }

    private void preventProxySubjectLeakForAllArgs(IInvocation invocation)
    {
      for (int i = 0; i < invocation.Arguments.Length; i++)
      {
        var currentArg = invocation.Arguments[i];
        invocation.Arguments[i] = ProxyContext.Current.SubjectProxyMapping.TryFindProxy(currentArg) ?? currentArg;
      }
    }
  }
}