using Ax.Fw.SharedTypes.Attributes;

namespace Ax.Fw.Storage.Tests.Data;

[SimpleDocument("simple-record")]
internal record DataRecord(int Id, string Name)
{
  public string GetStorageKey() => $"{Id}.{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
  public static int? GetIdFromStorageKey(string _storageKey)
  {
    var split = _storageKey.Split('.', StringSplitOptions.RemoveEmptyEntries);
    if (split.Length != 2)
      return null;

    if (!int.TryParse(split[0], out var projectId))
      return null;

    return projectId;
  }
};

