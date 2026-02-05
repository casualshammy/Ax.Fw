namespace Ax.Fw.Storage.Data;

public record BlobEntryMeta(
  long DocId,
  string Namespace,
  string Key,
  DateTimeOffset LastModified,
  DateTimeOffset Created,
  long Version,
  long RawLength);
