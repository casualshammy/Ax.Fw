using Ax.Fw.Storage.Data;

namespace Ax.Fw.Storage.Interfaces;

public interface IStorageRetentionRule
{
  public IReadOnlySet<BlobEntryMeta> Apply(
    SqliteBlobStorage _storage);
}
