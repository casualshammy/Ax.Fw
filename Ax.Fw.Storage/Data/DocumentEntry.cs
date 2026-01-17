namespace Ax.Fw.Storage.Data;

public record DocumentEntry<T>(
  long DocId,
  string Namespace,
  string Key,
  DateTimeOffset LastModified,
  DateTimeOffset Created,
  long Version,
  T Data);
