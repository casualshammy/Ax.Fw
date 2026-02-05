using Ax.Fw.Storage.Interfaces;
using System.Data;

namespace Ax.Fw.Storage.Data.Retention;

public record StorageRetentionRuleTotalSize(
  string Namespace,
  long TotalSizeInBytes) : IStorageRetentionRule
{
  public IReadOnlySet<BlobEntryMeta> Apply(
    SqliteBlobStorage _storage)
  {
    var entries = new LinkedList<BlobEntryMeta>(_storage
      .ListBlobsMeta(Namespace)
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