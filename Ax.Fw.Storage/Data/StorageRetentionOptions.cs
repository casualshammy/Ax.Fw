namespace Ax.Fw.Storage.Data;

public record StorageRetentionOptions(
  IReadOnlyList<StorageRetentionRule> Rules,
  TimeSpan? ScanInterval,
  Action<IReadOnlySet<DocumentEntryMeta>>? OnDocsDeleteCallback = null);
