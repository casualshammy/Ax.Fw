using Ax.Fw.Cache.Parts;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Cache;

public class FileCache
{
  private readonly Subject<SemaphoreSlim?> p_cleanReqFlow;
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
    TimeSpan? _cleanUpInterval,
    bool _generateStatFile = false)
  {
    p_lifetime = _lifetime;
    p_folder = _folder;
    p_ttl = _filesTtl;
    p_maxFolderSize = _maxFolderSize;

    p_cleanScheduler = _lifetime.ToDisposeOnEnded(new EventLoopScheduler());
    p_cleanReqFlow = _lifetime.ToDisposeOnEnded(new Subject<SemaphoreSlim?>());

    IObservable<SemaphoreSlim?> cleanFlow;
    if (_cleanUpInterval != null)
    {
      cleanFlow = Observable
        .Interval(_cleanUpInterval.Value, p_cleanScheduler)
        .StartWithDefault()
        .Select(_ => (SemaphoreSlim?)null);
    }
    else
    {
      cleanFlow = Observable.Empty<SemaphoreSlim?>();
    }

    cleanFlow
#if !DEBUG
      .Delay(TimeSpan.FromMinutes(1), p_cleanScheduler)
#endif
      .Merge(p_cleanReqFlow)
      .ObserveOn(p_cleanScheduler)
      .Subscribe(_flag =>
      {
        try
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
        }
        finally
        {
          _flag?.Release();
        }
      }, _lifetime);

    if (_generateStatFile)
    {
      Observable
        .Interval(TimeSpan.FromHours(1))
        .StartWithDefault()
        .ToUnit()
        .ObserveOnThreadPool()
        .Subscribe(_ =>
        {
          var sw = Stopwatch.StartNew();
          var dirInfo = new DirectoryInfo(p_folder);
          var totalFileCount = 0L;
          var totalFolderCount = 0L;
          var totalSize = 0L;
          foreach (var entry in dirInfo.GetFileSystemInfos("*", SearchOption.AllDirectories))
          {
            try
            {
              if (entry is FileInfo fileInfo)
              {
                ++totalFileCount;
                totalSize += fileInfo.Length;
              }
              else if (entry is DirectoryInfo)
              {
                ++totalFolderCount;
              }
            }
            catch (Exception)
            {
              // ignore errors
            }
          }
          var stats = new FileCacheStatFile(totalFolderCount, totalFileCount, totalSize, sw.Elapsed.TotalMilliseconds, DateTimeOffset.UtcNow);
          var json = JsonSerializer.Serialize(stats, FileCacheJsonCtx.Default.FileCacheStatFile);
          File.WriteAllText(Path.Combine(p_folder, "cache-stats.json"), json, Encoding.UTF8);
        }, _lifetime);
    }
  }

  public async Task StoreAsync(
    string _key,
    Stream _stream,
    bool _throwExceptions = false,
    CancellationToken _ct = default)
  {
    if (!_stream.CanRead)
    {
      if (_throwExceptions)
        throw new IOException("Can't read stream!");

      return;
    }

    var folder = GetFolderForKey(_key, out var hash);
    var file = Path.Combine(folder, hash);
    var tmpFile = Path.Combine(folder, $"{hash}_{Random.Shared.Next()}.tmp");

    try
    {
      if (!Directory.Exists(folder))
        Directory.CreateDirectory(folder);

      using (var fileStream = File.Open(tmpFile, FileMode.Create, FileAccess.Write, FileShare.None))
        await _stream.CopyToAsync(fileStream, _ct);

      File.Move(tmpFile, file, true);
    }
    catch (Exception)
    {
      if (_throwExceptions)
        throw;
    }
    finally
    {
      new FileInfo(tmpFile).TryDelete();
    }
  }

  public bool TryStore(string _key, Stream _stream, out Exception? _ex)
  {
    if (!_stream.CanRead)
    {
      _ex = new IOException("Can't read stream!");
      return false;
    }

    var folder = GetFolderForKey(_key, out var hash);
    var file = Path.Combine(folder, hash);
    var tmpFile = Path.Combine(folder, $"{hash}_{Random.Shared.Next()}.tmp");

    try
    {
      if (!Directory.Exists(folder))
        Directory.CreateDirectory(folder);

      using (var fileStream = File.Open(tmpFile, FileMode.Create, FileAccess.Write, FileShare.None))
        _stream.CopyTo(fileStream);

      File.Move(tmpFile, file, true);

      _ex = null;
      return false;
    }
    catch (Exception ex)
    {
      _ex = ex;
      return false;
    }
    finally
    {
      new FileInfo(tmpFile).TryDelete();
    }
  }

  public Stream? Get(string _key)
  {
    if (!IsKeyExists(_key, out var path, out _))
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

  public bool TryGet(
    string _key,
    [NotNullWhen(true)] out Stream? _stream,
    [NotNullWhen(true)] out string? _filePath,
    [NotNullWhen(true)] out string? _hash)
  {
    _stream = null;
    _filePath = null;
    _hash = null;

    if (!IsKeyExists(_key, out var path, out var hash))
      return false;

    try
    {
      _stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
      _filePath = path;
      _hash = hash;
      return true;
    }
    catch
    {
      return false;
    }
  }

  public bool IsKeyExists(
    string _key,
    [NotNullWhen(true)] out string? _path,
    [NotNullWhen(true)] out string? _hash)
  {
    _path = null;
    _hash = null;

    var folder = GetFolderForKey(_key, out var hash);
    var file = new FileInfo(Path.Combine(folder, hash));
    if (!file.Exists)
      return false;

    var now = DateTimeOffset.UtcNow;
    if (now - file.LastWriteTimeUtc > p_ttl)
    {
      file.TryDelete();
      return false;
    }

    _path = file.FullName;
    _hash = hash;
    return true;
  }

  /// <summary>
  /// Clean-up files: removes files older than TTL and removes newer files if occupied space is bigger than MaxFolderSize
  /// </summary>
  public void RequestCleanFiles() => p_cleanReqFlow.OnNext(null);

  public async Task CleanFilesWaitAsync(CancellationToken _ct)
  {
    using var flag = new SemaphoreSlim(0, 1);
    p_cleanReqFlow.OnNext(flag);

    await flag.WaitAsync(_ct);
  }

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
