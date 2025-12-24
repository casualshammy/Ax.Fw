namespace Ax.Fw.App.Interfaces;

public interface ITmpFileManager
{
  IDisposable GetTmpFilePath(TimeSpan _ttl, string? _extension, out string _filePath);

  void ReleaseFilePath(string _path);
}
