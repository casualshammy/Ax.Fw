namespace Ax.Fw.Storage.Data;

public record BlobEntry<T>(
  long DocId,
  string Namespace,
  string Key,
  DateTimeOffset LastModified,
  DateTimeOffset Created,
  long Version,
  long RawLength,
  T Data);