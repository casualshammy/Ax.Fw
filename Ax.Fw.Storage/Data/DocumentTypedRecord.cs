namespace Ax.Fw.Storage.Data;

public record DocumentTypedRecord<T>(int RecordId, int DocId, string TableId, string? RecordKey, DateTimeOffset LastModified, T Data);
