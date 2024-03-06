using System.Text.Json.Serialization;

namespace Ax.Fw.Cache.Parts;

[JsonSourceGenerationOptions(
  WriteIndented = true)]
[JsonSerializable(typeof(FileCacheStatFile))]
internal partial class FileCacheJsonCtx : JsonSerializerContext
{

}
