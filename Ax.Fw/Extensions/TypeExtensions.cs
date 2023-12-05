using System;
using System.Collections.Generic;

namespace Ax.Fw.Extensions;

public static class TypeExtensions
{
  public static IEnumerable<Type> ParentTypes(this Type _type)
  {
    if (_type.BaseType != null)
    {
      yield return _type.BaseType;
      foreach (Type b in _type.BaseType.ParentTypes())
        yield return b;
    }
  }

}
