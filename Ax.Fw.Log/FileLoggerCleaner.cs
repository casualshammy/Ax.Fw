using System.Collections.Concurrent;
using System.IO.Compression;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;

namespace Ax.Fw.Log;

public class FileLoggerCleaner : DisposableStack
{
  private readonly Subject<Unit> p_purgeReqFlow = new();

  private FileLoggerCleaner(
    DirectoryInfo _directory,
    bool _recursive,
    Regex _rotateFileNamePattern,
    TimeSpan _logTtl,
    bool _gzipFiles,
    TimeSpan? _rotateInterval = null,
    Action<FileInfo>? _onFileDeleted = null)
  {
    var scheduler = ToDisposeOnEnded(new EventLoopScheduler());

    IObservable<Unit> observable;
    if (_rotateInterval != null)
    {
      observable = Observable
        .Interval(_rotateInterval.Value, scheduler)
        .Select(_ => Unit.Default)
        .StartWith(Unit.Default)
        .Merge(p_purgeReqFlow);
    }
    else
    {
      observable = Observable
        .Return(Unit.Default)
        .Concat(p_purgeReqFlow);
    }

    var subs = observable
      .ObserveOn(scheduler)
      .Subscribe(_ =>
      {
        var now = DateTimeOffset.UtcNow;
        FileInfo[]? files = null;
        try
        {
          files = _recursive ?
            _directory.GetFiles("*", SearchOption.AllDirectories) :
            _directory.GetFiles("*", SearchOption.TopDirectoryOnly);
        }
        catch { }

        if (files == null)
          return;

        FileInfo? newestFile = null;
        var filesToGzip = new Dictionary<FileInfo, bool>();
        foreach (var file in files)
        {
          if (!_rotateFileNamePattern.IsMatch(file.Name))
            continue;

          filesToGzip[file] = true;
          if (newestFile == null)
          {
            newestFile = file;
            filesToGzip[newestFile] = false;
          }
          else if (file.LastWriteTimeUtc > newestFile.LastWriteTimeUtc)
          {
            filesToGzip[newestFile] = true;
            newestFile = file;
            filesToGzip[newestFile] = false;
          }

          if (now - file.LastWriteTimeUtc <= _logTtl)
            continue;

          try
          {
            file.Delete();
            file.Refresh();
            _onFileDeleted?.Invoke(file);
          }
          catch { }
        }
        if (_gzipFiles)
        {
          foreach (var (fileInfo, delete) in filesToGzip)
          {
            if (!delete)
              continue;
            if (!fileInfo.Exists)
              continue;

            try
            {
              var path = $"{fileInfo.FullName[..(fileInfo.FullName.Length - fileInfo.Extension.Length)]}.gzip";
              using (var logFileStream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
              using (var gzipFileStream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
              using (var gzipStream = new GZipStream(gzipFileStream, CompressionMode.Compress, true))
                logFileStream.CopyTo(gzipStream, 80192);

              fileInfo.Delete();
              fileInfo.Refresh();
            }
            catch { }
          }
        }
      });

    ToDispose(subs);
  }

  /// <summary>
  /// Creates log watcher. Logs in <paramref name="_directory"/> older than <paramref name="_logTtl"/> will be purged every <paramref name="_rotateInterval"/>
  /// <para></para>
  /// If <paramref name="_rotateInterval"/> is null, no periodic purges will be performed. In that case use <see cref="Purge"/> method
  /// </summary>
  public static FileLoggerCleaner Create(
    DirectoryInfo _directory,
    bool _recursive,
    Regex _logFileNamePattern,
    TimeSpan _logTtl,
    bool _gzipFiles,
    TimeSpan? _rotateInterval = null,
    Action<FileInfo>? _onFileDeleted = null)
    => new(_directory, _recursive, _logFileNamePattern, _logTtl, _gzipFiles, _rotateInterval, _onFileDeleted);

  public void Purge() => p_purgeReqFlow.OnNext(Unit.Default);

}
