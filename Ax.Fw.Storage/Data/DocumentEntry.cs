namespace Ax.Fw.Storage.Data;

public record DocumentEntry<T>(
  int DocId,
  string Namespace,
  string Key,
  DateTimeOffset LastModified,
  DateTimeOffset Created,
  long Version,
  T Data);
