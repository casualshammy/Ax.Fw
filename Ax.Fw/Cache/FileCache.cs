using Ax.Fw.Cache.Parts;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Data.Cache;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Streams;
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Cache;

public class FileCache
{
  private const int HEADER_SIZE = 512;
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
    TimeSpan? _cleanUpInterval)
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
  }

  public async Task StoreAsync(
    string _key,
    Stream _inStream,
    string? _mime = null,
    bool _throwExceptions = false,
    CancellationToken _ct = default)
  {
    var folder = GetFolderForKey(_key, out var hash);
    var fileName = GetFileNameForHash(hash);

    var filePath = Path.Combine(folder, fileName);
    var tmpFilePath = Path.Combine(folder, $"{hash}_{Random.Shared.Next():X}.tmp");

    try
    {
      Directory.CreateDirectory(folder);

      using (var fileStream = File.Open(tmpFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
      {
        Span<byte> headerBytes = new byte[HEADER_SIZE];
        var stringLength = Encoding.UTF8.GetBytes(_mime ?? MimeTypes.Bin.Mime, headerBytes[2..]);
        if (!BitConverter.TryWriteBytes(headerBytes, (ushort)stringLength))
          throw new InvalidOperationException("Failed to write MIME length to header.");

        await fileStream.WriteAsync(headerBytes.ToArray(), _ct);

        await _inStream.CopyToAsync(fileStream, _ct);
      }

      File.Move(tmpFilePath, filePath, true);
    }
    catch (Exception)
    {
      if (_throwExceptions)
        throw;
    }
    finally
    {
      new FileInfo(tmpFilePath).TryDelete();
    }
  }

  public bool TryGet(
    string _key,
    [NotNullWhen(true)] out Stream? _stream,
    [NotNullWhen(true)] out FileCacheEntryMeta? _meta)
  {
    _stream = null;
    _meta = null;

    if (!IsKeyExists(_key, out var path, out var hash))
      return false;

    try
    {
      var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
      var headerBytes = ArrayPool<byte>.Shared.Rent(HEADER_SIZE);
      try
      {
        stream.ReadExactly(headerBytes, 0, HEADER_SIZE);
        var stringLength = BitConverter.ToUInt16(headerBytes, 0);
        var mime = Encoding.UTF8.GetString(headerBytes, 2, stringLength);

        _stream = new StreamWrapper(stream, stream.Length - HEADER_SIZE, true);
        _meta = new FileCacheEntryMeta(path, hash, mime);

        return true;
      }
      finally
      {
        ArrayPool<byte>.Shared.Return(headerBytes);
      }
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
    var fileName = GetFileNameForHash(hash);

    var file = new FileInfo(Path.Combine(folder, fileName));
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

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static string GetFileNameForHash(string _hash) => $"{_hash}.v2";

}
