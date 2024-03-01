using System.Text.Json.Serialization;

namespace Ax.Fw.Cache.Parts;

[JsonSerializable(typeof(FileCacheStatFile))]
internal partial class FileCacheJsonCtx : JsonSerializerContext
{

}
