using Ax.Fw.Storage.Tests.Data;
using System.Text.Json.Serialization;

namespace Ax.Fw.Storage.Tests.JsonCtx;

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(DataRecord))]
[JsonSerializable(typeof(InterfacesRecord))]
internal partial class DocumentJsonCtx : JsonSerializerContext {}
