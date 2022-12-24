using Newtonsoft.Json.Linq;

namespace Ax.Fw.Storage.Data;

public record DocumentRecord(int RecordId, int DocId, string TableId, string? RecordKey, DateTimeOffset LastModified, JToken Data);
