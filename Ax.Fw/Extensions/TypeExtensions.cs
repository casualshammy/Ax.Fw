using System;
using System.Collections.Generic;

namespace Ax.Fw.Extensions
{
    public static class TypeExtensions
    {
        public static IEnumerable<Type> ParentTypes(this Type type)
        {
            if (type.BaseType != null)
            {
                yield return type.BaseType;
                foreach (Type b in type.BaseType.ParentTypes())
                    yield return b;
            }
        }
    }
}
