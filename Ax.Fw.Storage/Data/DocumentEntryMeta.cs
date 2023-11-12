namespace Ax.Fw.Storage.Data;

public record DocumentEntryMeta(
  int DocId,
  string Namespace,
  string Key,
  DateTimeOffset LastModified,
  DateTimeOffset Created,
  long Version);
