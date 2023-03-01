using Ax.Fw.SharedTypes.Attributes;
using Ax.Fw.Storage.Data;
using Ax.Fw.Storage.Interfaces;
using Ax.Fw.Storage.StorageTypes;
using System.Collections.Concurrent;
using System.Reflection;

namespace Ax.Fw.Storage.Extensions;

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
  public static DocumentStorage ToCached(this DocumentStorage _storage, int _maxValuesCached, TimeSpan _cacheTtl)
  {
    var cacheMaxValues = _maxValuesCached - _maxValuesCached / 10;
    var cacheOverhead = _maxValuesCached / 10;

    return new CachedSqliteDocumentStorage(_storage, cacheMaxValues, cacheOverhead, _cacheTtl);
  }

  /// <summary>
  /// Apply document retention rules to storage.
  /// <br/>
  /// Storage will be periodically scanned. Too old documents will be removed during scans.
  /// </summary>
  /// <param name="_documentMaxAgeFromCreation">All documents older than this value will be removed</param>
  /// <param name="_scanInterval">How often to perform scans. Default is 10 min</param>
  /// <param name="_onDocsDeleteCallback">This callback will be called when documents are deleted</param>
  /// <returns></returns>
  public static DocumentStorage WithRetentionRules(
    this DocumentStorage _storage,
    TimeSpan? _documentMaxAgeFromCreation = null,
    TimeSpan? _documentMaxAgeFromLastChange = null,
    TimeSpan? _scanInterval = null,
    Action<HashSet<DocumentEntryMeta>>? _onDocsDeleteCallback = null)
  {
    return new DocumentStorageWithRetentionRules(_storage, _documentMaxAgeFromCreation, _documentMaxAgeFromLastChange, _scanInterval, _onDocsDeleteCallback);
  }

}
