using Ax.Fw.Cache.Parts;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Data.Workers;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Workers;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Cache;

public class FileCache
{
  class StoreTask
  {
    public StoreTask(Stream _data, string _key, CancellationToken _token)
    {
      Data = _data;
      Key = _key;
      Token = _token;
    }

    public Stream Data { get; }
    public string Key { get; }
    public CancellationToken Token { get; }

  }

  class GetTask
  {
    public GetTask(string _key, CancellationToken _token)
    {
      Key = _key;
      Token = _token;
    }

    public string Key { get; }
    public CancellationToken Token { get; }

  }

  private readonly WorkerTeam<StoreTask, Unit> p_storeTeam;
  private readonly WorkerTeam<GetTask, Stream> p_getTeam;
  private readonly string p_folder;
  private readonly TimeSpan p_ttl;
  private readonly Subject<StoreTask> p_storeTasksFlow;
  private readonly Subject<GetTask> p_getTasksFlow;

  public FileCache(
    IReadOnlyLifetime _lifetime,
    string _folder,
    TimeSpan _ttl,
    TimeSpan _cleanUpInterval)
  {
    p_folder = _folder;
    p_ttl = _ttl;

    var scheduler = _lifetime.DisposeOnCompleted(new EventLoopScheduler());
    p_storeTasksFlow = _lifetime.DisposeOnCompleted(new Subject<StoreTask>());
    p_getTasksFlow = _lifetime.DisposeOnCompleted(new Subject<GetTask>());

    var schedulers = new IScheduler[] { scheduler };
    p_storeTeam = WorkerTeam.Run(p_storeTasksFlow, StoreInternalAsync, StorePenaltyAsync, _lifetime, schedulers);
    p_getTeam = WorkerTeam.Run<GetTask, Stream>(p_getTasksFlow, GetInternalAsync, GetPenaltyAsync, _lifetime, schedulers);

    Observable
      .Interval(_cleanUpInterval, scheduler)
      .StartWithDefault()
#if !DEBUG
      .Delay(TimeSpan.FromMinutes(1), scheduler)
#endif
      .ObserveOn(scheduler)
      .Subscribe(_ =>
      {
        var now = DateTimeOffset.UtcNow;
        if (!Directory.Exists(p_folder))
          return;

        var files = Directory.EnumerateFiles(p_folder, "*.json", SearchOption.AllDirectories);
        foreach (var jsonFile in files)
        {
          try
          {
            var dataFile = jsonFile[..(jsonFile.Length - 5)];
            if (!File.Exists(dataFile))
            {
              File.Delete(jsonFile);
              continue;
            }

            var json = File.ReadAllText(jsonFile, Encoding.UTF8);
            var info = JsonConvert.DeserializeObject<StoredFileInfo>(json);
            if (now - info.LastWrite > _ttl)
            {
              File.Delete(dataFile);
              File.Delete(jsonFile);
            }
          }
          catch (Exception ex)
          {
            // don't care
          }
        }
      }, _lifetime);
  }

  public async Task Store(string _key, Stream _stream, CancellationToken _ct)
  {
    await p_storeTeam.DoWork(new StoreTask(_stream, _key, _ct));
  }

  public async Task<Stream?> Get(string _key, CancellationToken _ct)
  {
    return await p_getTeam.DoWork(new GetTask(_key, _ct));
  }

  private async Task<bool> StoreInternalAsync(JobContext<StoreTask, Unit> _ctx)
  {
    var job = _ctx.JobInfo.Job;
    if (!job.Data.CanRead)
      throw new IOException("Can't read stream!");

    var folder = GetFolderForKey(job.Key, out var hash);
    if (!Directory.Exists(folder))
      Directory.CreateDirectory(folder);

    var file = Path.Combine(folder, hash);
    var jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new StoredFileInfo(DateTimeOffset.UtcNow)));

    using var fileStream = File.Open(file, FileMode.Create, FileAccess.Write, FileShare.None);
    using var jsonStream = File.Open($"{file}.json", FileMode.Create, FileAccess.Write, FileShare.None);
    await job.Data.CopyToAsync(fileStream, job.Token);
    await jsonStream.WriteAsync(jsonBytes, job.Token);

    return true;
  }

  private async Task<PenaltyInfo> StorePenaltyAsync(JobFailContext<StoreTask> _ctx)
  {
    if (_ctx.FailedCounter > 1)
      return new PenaltyInfo(false, TimeSpan.Zero);

    return new PenaltyInfo(true, TimeSpan.FromMilliseconds(250));
  }

  private async Task<Stream?> GetInternalAsync(JobContext<GetTask, Stream> _ctx)
  {
    var job = _ctx.JobInfo.Job;
    var folder = GetFolderForKey(job.Key, out var hash);
    if (!Directory.Exists(folder))
      return null;

    var file = Path.Combine(folder, hash);
    if (!File.Exists(file))
      return null;

    var rawJsonPath = $"{file}.json";
    if (!File.Exists(rawJsonPath))
    {
      File.Delete(file);
      return null;
    }

    var now = DateTimeOffset.UtcNow;
    var rawJson = File.ReadAllText(rawJsonPath, Encoding.UTF8);
    var info = JsonConvert.DeserializeObject<StoredFileInfo>(rawJson);
    if (now - info.LastWrite > p_ttl)
    {
      File.Delete(file);
      File.Delete(rawJsonPath);
      return null;
    }

    return File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
  }

  private async Task<PenaltyInfo> GetPenaltyAsync(JobFailContext<GetTask> _ctx)
  {
    if (_ctx.FailedCounter > 1)
      return new PenaltyInfo(false, TimeSpan.Zero);

    return new PenaltyInfo(true, TimeSpan.FromMilliseconds(250));
  }

  private string GetFolderForKey(string _key, out string _hash)
  {
    _hash = Cryptography.CalculateSHAHash(_key, SharedTypes.Data.HashComplexity.Bit256);
    var folder = Path.Combine(p_folder, _hash[..2], _hash[2..2]);
    return folder;
  }

}
