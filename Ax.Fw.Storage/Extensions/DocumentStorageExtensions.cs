using Ax.Fw.SharedTypes.Attributes;
using System.Collections.Concurrent;
using System.Reflection;

namespace Ax.Fw.Storage.Extensions;

internal static class DocumentStorageExtensions
{
  private static readonly ConcurrentDictionary<Type, string> p_namespacePerType = new();

  internal static string GetNamespaceFromType(this Type _type)
  {
    if (p_namespacePerType.TryGetValue(_type, out var ns))
      return ns;

    ns = _type.GetCustomAttribute<SimpleDocumentAttribute>()?.Namespace;

    if (ns == null)
    {
      var underlyingType = Nullable.GetUnderlyingType(_type);
      if (underlyingType != null)
        ns = $"autotype_nullable_{underlyingType.FullName?.ToLower() ?? underlyingType.Name.ToLower()}";
      else
        ns = $"autotype_{_type.FullName?.ToLower() ?? _type.Name.ToLower()}";
    }

    p_namespacePerType[_type] = ns;
    return ns;
  }

}
