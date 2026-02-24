using Ax.Fw.Storage.Interfaces;
using System.Data;

namespace Ax.Fw.Storage.Data.Retention;

/// <summary>
/// Represents a storage retention rule that deletes the oldest blobs in a namespace until the total size does not
/// exceed a specified limit.
/// </summary>
/// <remarks>This rule is typically used to enforce storage quotas by removing the oldest blobs first. When
/// applied, it evaluates all blobs matching the specified criteria and deletes the oldest ones until the total size
/// constraint is satisfied.</remarks>
/// <param name="Namespace">The namespace to which the retention rule applies. Can be null to match all namespaces.</param>
/// <param name="Key">A pattern used to match blob keys within the namespace. Can be null to match all keys.</param>
/// <param name="TotalSizeInBytes">The maximum allowed total size, in bytes, for blobs matching the specified namespace and key pattern. Blobs are
/// deleted until the total size is less than or equal to this value.</param>
public record StorageRetentionRuleTotalSize(
  string? Namespace,
  LikeExpr? Key,
  long TotalSizeInBytes) : IStorageRetentionRule
{
  public IReadOnlySet<BlobEntryMeta> Apply(
    SqliteBlobStorage _storage)
  {
    var entries = new LinkedList<BlobEntryMeta>(_storage
      .ListBlobsMeta(Namespace, Key)
      .OrderBy(_ => _.LastModified));

    var result = new HashSet<BlobEntryMeta>();

    if (entries.Count == 0)
      return result;

    var totalSize = entries.Sum(_ => _.RawLength);
    while (totalSize > TotalSizeInBytes)
    {
      var entryToDelete = entries.FirstOrDefault();
      if (entryToDelete == null)
        break;

      entries.RemoveFirst();

      _storage.DeleteBlobs(entryToDelete.Namespace, entryToDelete.Key);
      totalSize -= entryToDelete.RawLength;

      result.Add(entryToDelete);
    }

    return result;
  }
}