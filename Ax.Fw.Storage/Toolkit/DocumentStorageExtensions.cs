using Ax.Fw.Storage.Attributes;
using Ax.Fw.Storage.Extensions;
using Ax.Fw.Storage.Interfaces;
using System.Collections.Concurrent;
using System.Reflection;

namespace Ax.Fw.Storage.Toolkit;

public static class DocumentStorageExtensions
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

  /// <summary>
  /// Attach in-memory cache to this <see cref="IDocumentStorage"/>
  /// <br/>
  /// Cache is useful if you use reading more often than writing
  /// </summary>
  /// <param name="_maxValuesCached">Max number of values to store in cache</param>
  /// <returns></returns>
  public static IDocumentStorage ToCached(this IDocumentStorage _storage, int _maxValuesCached, TimeSpan _cacheTtl)
  {
    var cacheMaxValues = _maxValuesCached - _maxValuesCached / 10;
    var cacheOverhead = _maxValuesCached / 10;

    return new CachedSqliteDocumentStorage(_storage, cacheMaxValues, cacheOverhead, _cacheTtl);
  }

}
