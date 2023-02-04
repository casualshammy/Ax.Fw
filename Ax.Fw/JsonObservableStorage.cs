using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace Ax.Fw;

/// <summary>
/// Simple storage for data in JSON files
/// </summary>
public class JsonObservableStorage<T> : JsonStorage<T>, IJsonObservableStorage<T>
{
  class FileInfoChanges
  {
    public FileInfoChanges(FileInfo? _fileInfo, bool _changed)
    {
      FileInfo = _fileInfo;
      Changed = _changed;
    }

    public FileInfo? FileInfo { get; }
    public bool Changed { get; }
  }

  private readonly ReplaySubject<T?> p_changesFlow = new(1);

  /// <summary>
  ///
  /// </summary>
  /// <param name="_jsonFilePath">Path to JSON file. Can't be null or empty.</param>
  public JsonObservableStorage(ILifetime _lifetime, string _jsonFilePath) : base(_jsonFilePath)
  {
    _lifetime.DisposeOnCompleted(p_changesFlow);
    _lifetime.DisposeOnCompleted(Pool<EventLoopScheduler>.Get(out var scheduler));

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
        .Subscribe(_ => p_changesFlow.OnNext(GetDataOrDefaultSafe()), _lifetime);
  }

  /// <summary>
  /// IObservable of changes
  /// </summary>
  public IObservable<T?> Changes => p_changesFlow;

  private T? GetDataOrDefaultSafe()
  {
    try
    {
      bool fileExist = File.Exists(JsonFilePath);
      if (!fileExist)
        return default;

      return JsonConvert.DeserializeObject<T>(File.ReadAllText(JsonFilePath, Encoding.UTF8)) ?? default;
    }
    catch
    {
      return default;
    }
  }
}
