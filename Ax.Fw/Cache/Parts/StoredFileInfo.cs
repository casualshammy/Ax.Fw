using Newtonsoft.Json;
using System;

namespace Ax.Fw.Cache.Parts;

internal readonly struct StoredFileInfo
{
  [JsonConstructor]
  public StoredFileInfo(
    [JsonProperty(nameof(LastWrite))] DateTimeOffset _lastWrite)
  {
    LastWrite = _lastWrite;
  }

  public readonly DateTimeOffset LastWrite;
}
