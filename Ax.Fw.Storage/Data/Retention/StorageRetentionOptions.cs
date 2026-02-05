using Ax.Fw.Storage.Interfaces;

namespace Ax.Fw.Storage.Data.Retention;

public record StorageRetentionOptions(
  IReadOnlyList<IStorageRetentionRule> Rules,
  TimeSpan? ScanInterval,
  Action<IReadOnlySet<BlobEntryMeta>>? OnDocsDeleteCallback = null);
