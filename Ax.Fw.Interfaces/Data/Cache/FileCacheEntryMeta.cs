namespace Ax.Fw.SharedTypes.Data.Cache;

public record FileCacheEntryMeta(
  string FilePath,
  string Hash,
  string Mime);
