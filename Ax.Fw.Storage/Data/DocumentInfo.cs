namespace Ax.Fw.Storage.Data;

public record DocumentInfo(int DocId, string DocType, string Namespace, long Version, DateTimeOffset LastModified);
