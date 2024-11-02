using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ax.Fw.Tests.CompressTests;

[JsonSourceGenerationOptions(
  PropertyNameCaseInsensitive = false,
  UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<Sample>))]
internal partial class CompressJsonCtx : JsonSerializerContext
{ }
