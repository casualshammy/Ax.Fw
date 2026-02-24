using Ax.Fw.Storage.Extensions;
using Ax.Fw.Storage.Interfaces;

namespace Ax.Fw.Storage.Data.Retention;

/// <summary>
/// Represents a retention rule that identifies documents for deletion based on their age from creation or last
/// modification within a specified namespace and key pattern.
/// </summary>
/// <remarks>Use this rule to automate cleanup of stored documents that exceed specified age thresholds, helping
/// to manage storage usage and enforce data retention policies. Both creation and last modification age criteria are
/// optional; if both are specified, a document is selected for deletion if it exceeds either threshold.</remarks>
/// <param name="Namespace">The namespace to which the retention rule applies. Can be null to match all namespaces.</param>
/// <param name="Key">A pattern used to match document keys. Can be null to match all keys.</param>
/// <param name="DocumentMaxAgeFromCreation">The maximum allowed age of a document from its creation time. Documents older than this value may be selected for
/// deletion. Specify null to ignore this criterion.</param>
/// <param name="DocumentMaxAgeFromLastChange">The maximum allowed age of a document from its last modification time. Documents older than this value may be
/// selected for deletion. Specify null to ignore this criterion.</param>
public record StorageRetentionRuleAge(
  string? Namespace,
  LikeExpr? Key,
  TimeSpan? DocumentMaxAgeFromCreation,
  TimeSpan? DocumentMaxAgeFromLastChange) : IStorageRetentionRule
{
  /// <summary>
  /// Creates a new instance of the StorageRetentionRuleAge class for the specified document type, using the provided
  /// key pattern and maximum age constraints.
  /// </summary>
  /// <typeparam name="T">The type of document for which the retention rule is being created. The namespace of this type is used to scope
  /// the rule.</typeparam>
  /// <param name="_key">An optional pattern used to match document keys. If null, the rule applies to all documents of the specified type.</param>
  /// <param name="_documentMaxAgeFromCreation">The maximum allowed age for a document, measured from its creation time. If null, no maximum age from creation is
  /// enforced.</param>
  /// <param name="_documentMaxAgeFromLastChange">The maximum allowed age for a document, measured from its last modification time. If null, no maximum age from
  /// last change is enforced.</param>
  /// <returns>A <see cref="StorageRetentionRuleAge"/> instance configured for the specified document type and retention constraints.</returns>
  public static StorageRetentionRuleAge CreateForType<T>(
    LikeExpr? _key,
    TimeSpan? _documentMaxAgeFromCreation, 
    TimeSpan? _documentMaxAgeFromLastChange)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return new StorageRetentionRuleAge(ns, _key, _documentMaxAgeFromCreation, _documentMaxAgeFromLastChange);
  }

  public IReadOnlySet<BlobEntryMeta> Apply(
    SqliteBlobStorage _storage)
  {
    var result = new HashSet<BlobEntryMeta>();
    var now = DateTimeOffset.UtcNow;

    foreach (var entry in _storage.ListBlobsMeta(Namespace, Key))
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
