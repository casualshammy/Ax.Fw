using Ax.Fw.App.Interfaces;
using Ax.Fw.Extensions;
using Ax.Fw.Pools;
using Ax.Fw.SharedTypes.Interfaces;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;

namespace Ax.Fw.App.Modules.TmpFileManager;

internal class TmpFileManagerImpl : ITmpFileManager
{
  private readonly ConcurrentDictionary<string, long> p_tmpFiles = new();
  private readonly EventLoopScheduler p_checkSheduler;
  private readonly Subject<Unit> p_forceCheckSubj = new();
  private readonly IRxProperty<string> p_tmpDir;

  public TmpFileManagerImpl(
    ILog _log,
    IObservable<string?> _tmpDirFlow,
    IReadOnlyLifetime _lifetime)
  {
    _lifetime.ToDisposeOnEnded(SharedPool<EventLoopScheduler>.Get(out p_checkSheduler));

    p_tmpDir = _tmpDirFlow
      .DistinctUntilChanged()
      .Alive(_lifetime, p_checkSheduler, (_path, _life) =>
      {
        var tmpDirPath = GetTmpFolder(_path);

        void wipeFolder()
        {
          var tmpDirInfo = new DirectoryInfo(tmpDirPath);
          if (tmpDirInfo.Exists)
            foreach (var file in tmpDirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
              file.TryDelete();
        }

        wipeFolder();
        _life.DoOnEnded(() => wipeFolder());

        return tmpDirPath;
      })
      .WhereNotNull()
      .ToProperty(_lifetime, GetTmpFolder(null));

    p_tmpDir.Subscribe(_dir =>
    {
      _log.Info($"Using **tmp folder**: __{_dir}__");
    }, _lifetime);

    Observable
      .Interval(TimeSpan.FromMinutes(10), p_checkSheduler)
      .ToUnit()
      .Merge(p_forceCheckSubj)
      .Sample(TimeSpan.FromSeconds(10), p_checkSheduler)
      .ObserveOn(p_checkSheduler)
      .Subscribe(_u =>
      {
        long now = Environment.TickCount64;
        foreach (var (path, expires) in p_tmpFiles)
        {
          var fileInfo = new FileInfo(path);
          if (!fileInfo.Exists)
            p_tmpFiles.TryRemove(path, out _);
          else if (now > expires && fileInfo.TryDelete())
            p_tmpFiles.TryRemove(path, out _);
        }
      }, _lifetime);

    _lifetime.DoOnEnded(() =>
    {
      foreach (var (path, _) in p_tmpFiles)
      {
        var fileInfo = new FileInfo(path);
        if (fileInfo.Exists)
          fileInfo.TryDelete();
      }
    });
  }

  public IDisposable GetTmpFilePath(TimeSpan _ttl, string? _extension, out string _filePath)
  {
    var tmpDir = p_tmpDir.Value;
    if (!Directory.Exists(tmpDir))
      Directory.CreateDirectory(tmpDir);

    var tmpFilePath = Path.Combine(tmpDir, $"{Random.Shared.Next():X}-{Random.Shared.Next():X}.{_extension ?? "tmp"}");
    var expires = Environment.TickCount64 + (long)_ttl.TotalMilliseconds;
    p_tmpFiles.AddOrUpdate(tmpFilePath, expires, (_, _) => expires);
    _filePath = tmpFilePath;

    return Disposable.Create(() => ReleaseFilePath(tmpFilePath));
  }

  public void ReleaseFilePath(string _path)
  {
    p_checkSheduler.Schedule(() =>
    {
      if (p_tmpFiles.ContainsKey(_path))
        p_tmpFiles.AddOrUpdate(_path, 0L, (_, _) => 0L);

      p_forceCheckSubj.OnNext(Unit.Default);
    });
  }

  private static string GetTmpFolder(string? _path)
  {
    var tmpDirPath = _path;
    if (tmpDirPath == null)
    {
      var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
      tmpDirPath = Path.Combine(Path.GetTempPath(), assemblyName ?? Random.Shared.Next(100000000, 1000000000).ToString("X"));
    }
    return tmpDirPath;
  }

}
