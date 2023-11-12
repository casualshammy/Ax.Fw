using Ax.Fw.Extensions;
using Ax.Fw.JsonStorages.Parts;
using Ax.Fw.Pools;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.JsonStorages;

/// <summary>
/// Simple storage for data in JSON files
/// </summary>
public class JsonObservableStorageV2<T> : JsonStorageV2<T>, IJsonObservable<T?>
{
  private readonly ReplaySubject<T?> p_changesFlow = new(1);

  /// <summary>
  ///
  /// </summary>
  /// <param name="_jsonFilePath">Path to JSON file. Can't be null or empty.</param>
  public JsonObservableStorageV2(IReadOnlyLifetime _lifetime, string _jsonFilePath) : base(_jsonFilePath)
  {
    _lifetime.ToDisposeOnEnding(p_changesFlow);
    _lifetime.ToDisposeOnEnding(SharedPool<EventLoopScheduler>.Get(out var scheduler));

    Observable
      .Interval(TimeSpan.FromSeconds(1), scheduler)
      .StartWithDefault()
      .Scan(new FileInfoChanges(null, false), (_seed, _) =>
      {
        var newFileInfo = new FileInfo(_jsonFilePath);

        if (_seed.FileInfo == null)
          return new(newFileInfo, true);

        if (_seed.FileInfo.Exists != newFileInfo.Exists)
          return new(newFileInfo, true);

        if (newFileInfo.Exists)
        {
          if (_seed.FileInfo.LastWriteTimeUtc != newFileInfo.LastWriteTimeUtc)
            return new(newFileInfo, true);

          if (_seed.FileInfo.Length != newFileInfo.Length)
            return new(newFileInfo, true);
        }

        return new(newFileInfo, false);
      })
      .Where(_x => _x.Changed)
      .SelectAsync(async (_, _ct) =>
      {
        var data = await GetDataOrDefaultSafeAsync(_ct);
        p_changesFlow.OnNext(data);
      }, scheduler)
      .Subscribe(_lifetime);
  }

  /// <summary>
  /// IObservable of changes
  /// </summary>
  public IObservable<T?> Changes => p_changesFlow;

  public IDisposable Subscribe(IObserver<T?> _observer) => p_changesFlow.Subscribe(_observer);

  private async Task<T?> GetDataOrDefaultSafeAsync(CancellationToken _ct)
  {
    try
    {
      if (!File.Exists(JsonFilePath))
        return default;

      using (var fileStream = File.OpenRead(JsonFilePath))
        return await JsonSerializer.DeserializeAsync<T>(fileStream, cancellationToken: _ct) ?? default;
    }
    catch
    {
      return default;
    }
  }

}
