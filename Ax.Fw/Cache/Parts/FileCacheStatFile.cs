namespace Ax.Fw.Cache.Parts;

internal readonly record struct FileCacheStatFile(
  long TotalFolders,
  long TotalFiles,
  long TotalSizeBytes,
  double StatFileGenerationTimeMs);
