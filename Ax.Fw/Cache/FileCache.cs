using Ax.Fw.Cache.Parts;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Data.Workers;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Workers;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Cache;

public class FileCache
{
  private readonly Subject<Unit> p_cleanReqFlow;
  private readonly IReadOnlyLifetime p_lifetime;
  private readonly string p_folder;
  private readonly TimeSpan p_ttl;
  private readonly long p_maxFolderSize;
  private readonly EventLoopScheduler p_cleanScheduler;

  public FileCache(
    IReadOnlyLifetime _lifetime,
    string _folder,
    TimeSpan _filesTtl,
    long _maxFolderSize,
    TimeSpan? _cleanUpInterval)
  {
    p_lifetime = _lifetime;
    p_folder = _folder;
    p_ttl = _filesTtl;
    p_maxFolderSize = _maxFolderSize;

    p_cleanScheduler = _lifetime.ToDisposeOnEnded(new EventLoopScheduler());
    p_cleanReqFlow = _lifetime.ToDisposeOnEnded(new Subject<Unit>());

    IObservable<Unit> cleanFlow;
    if (_cleanUpInterval != null)
    {
      cleanFlow = Observable
        .Interval(_cleanUpInterval.Value, p_cleanScheduler)
        .StartWithDefault()
        .ToUnit();
    }
    else
    {
      cleanFlow = Observable.Empty<Unit>();
    }

    cleanFlow
#if !DEBUG
      .Delay(TimeSpan.FromMinutes(1), p_cleanScheduler)
#endif
      .Merge(p_cleanReqFlow)
      .ObserveOn(p_cleanScheduler)
      .Subscribe(_ =>
      {
        var now = DateTimeOffset.UtcNow;
        if (!Directory.Exists(p_folder))
          return;

        var enumerable = Directory
          .EnumerateFiles(p_folder, "*", SearchOption.AllDirectories)
          .Select(_ => new FileInfo(_))
          .OrderByDescending(_ => _.CreationTimeUtc);

        var folderSize = 0L;
        foreach (var file in enumerable)
        {
          if (file.Name.EndsWith(".tmp") && file.TryDelete())
            continue;
          if (now - file.LastWriteTimeUtc > p_ttl && file.TryDelete())
            continue;

          folderSize += file.Length;
          if (folderSize > p_maxFolderSize)
            file.TryDelete();
        }
      }, _lifetime);
  }

  public async Task StoreAsync(string _key, Stream _stream, CancellationToken _ct)
  {
    if (!_stream.CanRead)
      throw new IOException("Can't read stream!");

    var folder = GetFolderForKey(_key, out var hash);
    if (!Directory.Exists(folder))
      Directory.CreateDirectory(folder);

    var file = Path.Combine(folder, hash);
    var tmpFile = Path.Combine(folder, $"{hash}.tmp");
    try
    {
      using (var fileStream = File.Open(tmpFile, FileMode.Create, FileAccess.Write, FileShare.None))
        await _stream.CopyToAsync(fileStream, _ct);

      File.Move(tmpFile, file);
    }
    finally
    {
      new FileInfo(tmpFile).TryDelete();
    }
  }

  public void Store(string _key, Stream _stream)
  {
    if (!_stream.CanRead)
      throw new IOException("Can't read stream!");

    var folder = GetFolderForKey(_key, out var hash);
    if (!Directory.Exists(folder))
      Directory.CreateDirectory(folder);

    var file = Path.Combine(folder, hash);
    var tmpFile = Path.Combine(folder, $"{hash}.tmp");

    try
    {
      using (var fileStream = File.Open(tmpFile, FileMode.Create, FileAccess.Write, FileShare.None))
        _stream.CopyTo(fileStream);

      File.Move(tmpFile, file);
    }
    finally
    {
      new FileInfo(tmpFile).TryDelete();
    }
  }

  public Stream? Get(string _key)
  {
    if (!IsKeyExists(_key, out var path))
      return null;

    try
    {
      return File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    }
    catch
    {
      return null;
    }
  }

  public bool IsKeyExists(string _key, [NotNullWhen(true)] out string? _path)
  {
    var folder = GetFolderForKey(_key, out var hash);
    var file = new FileInfo(Path.Combine(folder, hash));
    if (!file.Exists)
    {
      _path = null;
      return false;
    }

    var now = DateTimeOffset.UtcNow;
    if (now - file.LastWriteTimeUtc > p_ttl)
    {
      file.TryDelete();
      _path = null;
      return false;
    }

    _path = file.FullName;
    return true;
  }

  /// <summary>
  /// Clean-up files: removes files older than TTL and removes newer files if occupied space is bigger than MaxFolderSize
  /// </summary>
  public void CleanFiles() => p_cleanReqFlow.OnNext();

  /// <summary>
  /// Delete all files from cache directory
  /// </summary>
  public void WipeFiles()
  {
    if (!Directory.Exists(p_folder))
      return;

    if (p_lifetime.IsCancellationRequested)
      return;

    p_cleanScheduler.Schedule(() =>
    {
      var enumerable = Directory
        .EnumerateFiles(p_folder, "*", SearchOption.AllDirectories)
        .Select(_ => new FileInfo(_));

      foreach (var file in enumerable)
        file.TryDelete();
    });
  }

  private string GetFolderForKey(string _key, out string _hash)
  {
    _hash = Cryptography.CalculateSHAHash(_key, SharedTypes.Data.HashComplexity.Bit256);
    var folder = Path.Combine(p_folder, _hash[..2], _hash[2..4]);
    return folder;
  }

}
