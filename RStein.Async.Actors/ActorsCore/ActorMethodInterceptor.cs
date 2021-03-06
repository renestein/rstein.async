﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using RStein.Async.Schedulers;
using RStein.Async.Tasks;

namespace RStein.Async.Actors.ActorsCore
{
  internal class ActorMethodInterceptor : IInterceptor, IEquatable<ActorMethodInterceptor>
  {
    private readonly ITaskScheduler m_primaryScheduler;
    private readonly ConditionalWeakTable<Object, StrandSchedulerDecorator> m_strandActorDictionary;

    public ActorMethodInterceptor(ITaskScheduler primaryScheduler)
    {
      m_primaryScheduler = primaryScheduler ?? throw new ArgumentNullException(nameof(primaryScheduler));
      m_strandActorDictionary = new ConditionalWeakTable<object, StrandSchedulerDecorator>();
    }

    public virtual bool Equals(ActorMethodInterceptor other)
    {
      return !ReferenceEquals(null, other);
    }

    public virtual void Intercept(IInvocation invocation)
    {
      var strand = getStrand(invocation.InvocationTarget);
      if (isVoidMethod(invocation.MethodInvocationTarget))
      {
        postTargetSub(strand, invocation);
      }
      else
      {
        Debug.Assert(isMethodReturningTask(invocation.MethodInvocationTarget));
        postTargetFunc(strand, invocation);
      }
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj))
      {
        return false;
      }
      if (ReferenceEquals(this, obj))
      {
        return true;
      }
      if (obj.GetType() != typeof (ActorMethodInterceptor))
      {
        return false;
      }

      return Equals((ActorMethodInterceptor) obj);
    }


    public override int GetHashCode() => GetType().GetHashCode();

    public static bool operator ==(ActorMethodInterceptor left, ActorMethodInterceptor right) => Equals(left, right);

    public static bool operator !=(ActorMethodInterceptor left, ActorMethodInterceptor right) => !Equals(left, right);

    private bool isVoidMethod(MethodInfo methodInvocation) => methodInvocation.ReturnType.Equals(typeof (void));

    private bool isMethodReturningTask(MethodInfo methodInvocation)
    {
      var returnMethodType = methodInvocation.ReturnType;
      return typeof (Task).IsAssignableFrom(returnMethodType);
    }

    private void postTargetSub(StrandSchedulerDecorator strand, IInvocation invocation)
    {
      void postAction()
      {
        invocation.GetConcreteMethodInvocationTarget()
          .Invoke(invocation.InvocationTarget, invocation.Arguments);
        //Problems with Castle implementation (call from other thread does not work after upgrade to 4.3.1. version)
        //invocation.Proceed();
      }

      strand.Post((Action) postAction);
    }

    private void postTargetFunc(StrandSchedulerDecorator strand, IInvocation invocation)
    {
      var methodInvocationTarget = invocation.MethodInvocationTarget;
      dynamic proxyTcs = null;

      proxyTcs = getProxyTcs(methodInvocationTarget);
      var isGenericReturnType = methodInvocationTarget.ReturnType.IsGenericType;

      invocation.ReturnValue = proxyTcs.Task;

      strand.Post(postFunc);

      Task postFunc()
      {
        Task resultTask = null;
        var hasException = false;

        try
        {
          resultTask = invocation.GetConcreteMethodInvocationTarget()
            .Invoke(invocation.InvocationTarget, invocation.Arguments) as Task;
          //Problems with Castle implementation (call from other thread does not work after upgrade to 4.3.1. version)
          //invocation.Proceed();
        }
        catch (Exception e)
        {
          hasException = true;
          if (resultTask == null)
          {
            resultTask = TaskEx.TaskFromException(e);
          }
        }
        finally
        {
          if (!hasException && isGenericReturnType)
          {
            TaskEx.PrepareTcsTaskFromExistingTask((dynamic) resultTask, proxyTcs);
          }
          else
          {
            TaskEx.PrepareTcsTaskFromExistingTask(resultTask, proxyTcs);
          }
        }

        return resultTask;
      }
    }

    private static dynamic getProxyTcs(MethodInfo methodInvocationTarget)
    {
      dynamic proxyTcs;

      if (methodInvocationTarget.ReturnType.IsGenericType)
      {
        var closedTcsType = typeof (TaskCompletionSource<>)
          .MakeGenericType(methodInvocationTarget.ReturnType
            .GetGenericArguments());

        proxyTcs = Activator.CreateInstance(closedTcsType);
      }
      else
      {
        proxyTcs = new TaskCompletionSource<Object>();
      }

      return proxyTcs;
    }

    private StrandSchedulerDecorator getStrand(object invocationTarget)
    {
      return m_strandActorDictionary.GetValue(invocationTarget, _ =>
                                                                {
                                                                  var strandScheduler = new StrandSchedulerDecorator(m_primaryScheduler);
                                                                  var externalProxyScheduler = new ProxyScheduler(strandScheduler);
                                                                  return strandScheduler;
                                                                });
    }
  }
}