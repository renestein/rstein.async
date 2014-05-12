﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Castle.Core.Interceptor;
using RStein.Async.Schedulers;

namespace RStein.Async.Examples.ActorsCore
{
  internal class ActorMethodInterceptor : IInterceptor, IEquatable<ActorMethodInterceptor>
  {
    private readonly ITaskScheduler m_primaryScheduler;
    private readonly ConditionalWeakTable<Object, StrandSchedulerDecorator> m_strandActorDictionary;

    public ActorMethodInterceptor(ITaskScheduler primaryScheduler)
    {
      if (primaryScheduler == null)
      {
        throw new ArgumentNullException("primaryScheduler");
      }

      m_primaryScheduler = primaryScheduler;
      m_strandActorDictionary = new ConditionalWeakTable<object, StrandSchedulerDecorator>();
    }

    public virtual bool Equals(ActorMethodInterceptor other)
    {
      return !ReferenceEquals(null, other);
    }

    public virtual void Intercept(IInvocation invocation)
    {
      StrandSchedulerDecorator strand = getStrand(invocation.InvocationTarget);
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
      if (obj.GetType() != typeof(ActorMethodInterceptor))
      {
        return false;
      }

      return Equals((ActorMethodInterceptor)obj);
    }


    public override int GetHashCode()
    {
      return GetType().GetHashCode();
    }

    public static bool operator ==(ActorMethodInterceptor left, ActorMethodInterceptor right)
    {
      return Equals(left, right);
    }

    public static bool operator !=(ActorMethodInterceptor left, ActorMethodInterceptor right)
    {
      return !Equals(left, right);
    }

    private bool isVoidMethod(MethodInfo methodInvocation)
    {
      return methodInvocation.ReturnType.Equals(typeof(void));
    }

    private bool isMethodReturningTask(MethodInfo methodInvocation)
    {
      Type returnMethodType = methodInvocation.ReturnType;
      return typeof(Task).IsAssignableFrom(returnMethodType);
    }

    private void postTargetSub(StrandSchedulerDecorator strand, IInvocation invocation)
    {
      Action action = invocation.Proceed;
      strand.Post(action);
    }

    private void postTargetFunc(StrandSchedulerDecorator strand, IInvocation invocation)
    {
      Func<Task> function = () =>
                            {
                              invocation.Proceed();
                              return invocation.ReturnValue as Task;
                            };
      strand.Post(function);
    }

    private StrandSchedulerDecorator getStrand(object invocationTarget)
    {
      return m_strandActorDictionary.GetValue(invocationTarget, _ => new StrandSchedulerDecorator(m_primaryScheduler));
    }
  }
}