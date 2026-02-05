using Ax.Fw.Storage.Extensions;
using Ax.Fw.Storage.Interfaces;

namespace Ax.Fw.Storage.Data.Retention;

public record StorageRetentionRuleAge(
  string Namespace,
  TimeSpan? DocumentMaxAgeFromCreation,
  TimeSpan? DocumentMaxAgeFromLastChange) : IStorageRetentionRule
{
  public static StorageRetentionRuleAge CreateForType<T>(
    TimeSpan? _documentMaxAgeFromCreation, 
    TimeSpan? _documentMaxAgeFromLastChange)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return new StorageRetentionRuleAge(ns, _documentMaxAgeFromCreation, _documentMaxAgeFromLastChange);
  }

  public IReadOnlySet<BlobEntryMeta> Apply(
    SqliteBlobStorage _storage)
  {
    var result = new HashSet<BlobEntryMeta>();
    var now = DateTimeOffset.UtcNow;

    foreach (var entry in _storage.ListBlobsMeta(Namespace))
    {
      var docAge = now - entry.Created;
      var docLastModifiedAge = now - entry.LastModified;
      if (DocumentMaxAgeFromCreation != null && docAge > DocumentMaxAgeFromCreation)
        result.Add(entry);
      else if (DocumentMaxAgeFromLastChange != null && docLastModifiedAge > DocumentMaxAgeFromLastChange)
        result.Add(entry);
    }

    foreach (var doc in result)
      _storage.DeleteBlobs(doc.Namespace, doc.Key, null, null);

    return result;
  }
};
