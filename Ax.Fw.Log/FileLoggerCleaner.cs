using System.Collections.Concurrent;
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

        foreach (var file in files)
          if (now - file.LastWriteTimeUtc > _logTtl && _rotateFileNamePattern.IsMatch(file.Name))
          {
            try
            {
              file.Delete();

              if (_onFileDeleted != null)
              {
                file.Refresh();
                _onFileDeleted.Invoke(file);
              }
            }
            catch { }
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
    TimeSpan? _rotateInterval = null,
    Action<FileInfo>? _onFileDeleted = null)
    => new(_directory, _recursive, _logFileNamePattern, _logTtl, _rotateInterval, _onFileDeleted);

  public void Purge() => p_purgeReqFlow.OnNext(Unit.Default);

}
