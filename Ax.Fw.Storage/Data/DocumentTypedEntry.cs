namespace Ax.Fw.Storage.Data;

public record DocumentTypedEntry<T>(
    int DocId,
    string Namespace,
    string Key,
    DateTimeOffset LastModified,
    DateTimeOffset Created,
    long Version,
    T Data);
