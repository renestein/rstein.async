using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace RStein.Async.Actors.ActorsCore
{
  public class ProxyGenerationHook : IProxyGenerationHook
  {
    public const string NO_VIRTUAL_MEMBER_MESSAGE = "Non virtual member function {0}-{1}";
    public const string UNKNOWN_RETURN_TYPE_MEMBER_MESSAGE = "Function with unknown return type {0}-{1}";

    public void NonProxyableMemberNotification(Type type, MemberInfo memberInfo)
    {
      Debug.WriteLine(NO_VIRTUAL_MEMBER_MESSAGE, type.FullName, memberInfo.Name);
    }

    public virtual bool ShouldInterceptMethod(Type type, MethodInfo methodInfo)
    {
      bool shouldIntercept = methodInfo.ReturnType.Equals(typeof (void)) ||
                             typeof (Task).IsAssignableFrom(methodInfo.ReturnType);

      if (!shouldIntercept)
      {
        Debug.WriteLine(UNKNOWN_RETURN_TYPE_MEMBER_MESSAGE, type.FullName, methodInfo.Name);
      }

      return shouldIntercept;
    }

    public virtual void MethodsInspected() {}

    public void NonVirtualMemberNotification(Type type, MemberInfo memberInfo)
    {
     
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
      if (obj.GetType() != typeof (ProxyGenerationHook))
      {
        return false;
      }
      return true;
    }

    public override int GetHashCode()
    {
      return GetType().GetHashCode();
    }

    public static bool operator ==(ProxyGenerationHook left, ProxyGenerationHook right)
    {
      return Equals(left, right);
    }

    public static bool operator !=(ProxyGenerationHook left, ProxyGenerationHook right)
    {
      return !Equals(left, right);
    }
  }
}