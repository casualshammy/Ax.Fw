using Ax.Fw.Storage.Extensions;

namespace Ax.Fw.Storage.Data;

public record StorageRetentionRule(
  string Namespace,
  TimeSpan? DocumentMaxAgeFromCreation,
  TimeSpan? DocumentMaxAgeFromLastChange)
{
  public static StorageRetentionRule CreateForType<T>(
    TimeSpan? _documentMaxAgeFromCreation, 
    TimeSpan? _documentMaxAgeFromLastChange)
  {
    var ns = typeof(T).GetNamespaceFromType();
    return new StorageRetentionRule(ns, _documentMaxAgeFromCreation, _documentMaxAgeFromLastChange);
  }
};
