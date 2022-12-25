using Newtonsoft.Json.Linq;

namespace Ax.Fw.Storage.Data;

public record DocumentEntry(
    int DocId, 
    string Namespace, 
    string Key,
    DateTimeOffset LastModified, 
    DateTimeOffset Created,
    long Version,
    JToken Data);
