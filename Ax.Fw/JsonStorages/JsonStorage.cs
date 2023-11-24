using Ax.Fw.Extensions;
using Ax.Fw.Pools;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.JsonStorages;

/// <summary>
/// Simple storage for data in JSON files
/// </summary>
public class JsonStorage<T> : IJsonStorage<T>, IObservable<T?>
{
  readonly record struct FileMetaInfo(bool Seen, bool Exists, DateTime LastWriteTimeUtc, long Length, bool Changed)
  {
    public static FileMetaInfo FromFileInfo(FileInfo _fileInfo, bool _changed)
    {
      if (_fileInfo.Exists)
        return new(true, _fileInfo.Exists, _fileInfo.LastWriteTimeUtc, _fileInfo.Length, _changed);
      else
        return new(true, _fileInfo.Exists, DateTime.MinValue, 0L, _changed);
    }
  };

  private readonly IObservable<T?> p_sharedObservable;
  private readonly JsonTypeInfo<T>? p_jsonTypeInfo;

  /// <summary>
  ///
  /// </summary>
  /// <param name="_jsonFilePath">Path to JSON file. Can't be null or empty.</param>
  public JsonStorage(
    string _jsonFilePath,
    JsonTypeInfo<T>? _jsonTypeInfo,
    IReadOnlyLifetime _lifetime)
  {
    if (string.IsNullOrWhiteSpace(_jsonFilePath))
      throw new ArgumentNullException(nameof(_jsonFilePath));

    p_jsonTypeInfo = _jsonTypeInfo;
    JsonFilePath = _jsonFilePath;

    var scheduler = _lifetime.ToDisposeOnEnded(new EventLoopScheduler());

    p_sharedObservable = Observable
      .Interval(TimeSpan.FromSeconds(1))
      .StartWithDefault()
      .ObserveOn(scheduler)
      .Scan(new FileMetaInfo(false, false, DateTime.MinValue, 0L, false), (_acc, _) =>
      {
        var newFileInfo = new FileInfo(_jsonFilePath);

        if (!_acc.Seen)
          return FileMetaInfo.FromFileInfo(newFileInfo, true);

        if (_acc.Exists != newFileInfo.Exists)
          return FileMetaInfo.FromFileInfo(newFileInfo, true);

        if (newFileInfo.Exists)
        {
          if (_acc.LastWriteTimeUtc != newFileInfo.LastWriteTimeUtc)
            return FileMetaInfo.FromFileInfo(newFileInfo, true);

          if (_acc.Length != newFileInfo.Length)
            return FileMetaInfo.FromFileInfo(newFileInfo, true);
        }

        return FileMetaInfo.FromFileInfo(newFileInfo, false);
      })
      .Where(_x => _x.Changed)
      .SelectAsync(async (_, _ct) =>
      {
        var data = await GetDataOrDefaultSafeAsync(_ct);
        return data;
      }, scheduler)
      .Publish()
      .RefCount();
  }

  /// <summary>
  /// Path to file
  /// </summary>
  public string JsonFilePath { get; }

  /// <summary>
  /// Loads data from JSON file
  /// </summary>
  /// <param name="_defaultFactory">
  /// If file doesn't exist, this method will be invoked to produce default value
  /// </param>
  /// <returns>
  /// Instance of <see cref="T"/>
  /// </returns>
  public async Task<T> ReadAsync(Func<CancellationToken, Task<T>> _defaultFactory, CancellationToken _ct)
  {
    if (!File.Exists(JsonFilePath))
      return await _defaultFactory(_ct);

    using (var fileStream = File.OpenRead(JsonFilePath))
    {
      try
      {
        if (p_jsonTypeInfo != null)
          return await JsonSerializer.DeserializeAsync(fileStream, p_jsonTypeInfo, _ct) ?? await _defaultFactory(_ct);
        else
          return await JsonSerializer.DeserializeAsync<T>(fileStream, cancellationToken: _ct) ?? await _defaultFactory(_ct);
      }
      catch
      {
        return await _defaultFactory(_ct);
      }
    }
  }

  /// <summary>
  /// Loads data from JSON file
  /// </summary>
  /// <param name="_defaultFactory">
  /// If file doesn't exist, this method will be invoked to produce default value
  /// </param>
  /// <returns>
  /// Instance of <see cref="T"/>
  /// </returns>
  public T Read(Func<T> _defaultFactory)
  {
    if (!File.Exists(JsonFilePath))
      return _defaultFactory();

    using (var fileStream = File.OpenRead(JsonFilePath))
    {
      try
      {
        if (p_jsonTypeInfo != null)
          return JsonSerializer.Deserialize(fileStream, p_jsonTypeInfo) ?? _defaultFactory();
        else
          return JsonSerializer.Deserialize<T>(fileStream) ?? _defaultFactory();
      }
      catch
      {
        return _defaultFactory();
      }
    }
  }

  public async Task WriteAsync(T? _data, CancellationToken _ct)
  {
    if (_data == null)
    {
      new FileInfo(JsonFilePath).TryDelete();
      return;
    }

    using var fileStream = File.Open(JsonFilePath, FileMode.Create);
    if (p_jsonTypeInfo != null)
      await JsonSerializer.SerializeAsync(fileStream, _data, p_jsonTypeInfo, _ct);
    else
      await JsonSerializer.SerializeAsync(fileStream, _data, cancellationToken: _ct);
  }

  public void Write(T? _data)
  {
    if (_data == null)
    {
      new FileInfo(JsonFilePath).TryDelete();
      return;
    }

    using var fileStream = File.Open(JsonFilePath, FileMode.Create);
    if (p_jsonTypeInfo != null)
      JsonSerializer.Serialize(fileStream, _data, p_jsonTypeInfo);
    else
      JsonSerializer.Serialize(fileStream, _data);
  }

  public IDisposable Subscribe(IObserver<T?> _observer) => p_sharedObservable.Subscribe(_observer);

  private async Task<T?> GetDataOrDefaultSafeAsync(CancellationToken _ct)
  {
    try
    {
      if (!File.Exists(JsonFilePath))
        return default;

      using (var fileStream = File.OpenRead(JsonFilePath))
      {
        if (p_jsonTypeInfo != null)
          return await JsonSerializer.DeserializeAsync(fileStream, p_jsonTypeInfo, _ct) ?? default;
        else
          return await JsonSerializer.DeserializeAsync<T>(fileStream, cancellationToken: _ct) ?? default;
      }
    }
    catch
    {
      return default;
    }
  }

}
