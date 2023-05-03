using Ax.Fw.Cache.Parts;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Data.Workers;
using Ax.Fw.SharedTypes.Interfaces;
using Ax.Fw.Workers;
using System;
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
  private readonly WorkerTeam<FileCacheStoreTask, Unit> p_storeTeam;
  private readonly WorkerTeam<FileCacheGetTask, Stream> p_getTeam;
  private readonly Subject<FileCacheStoreTask> p_storeTasksFlow;
  private readonly Subject<FileCacheGetTask> p_getTasksFlow;
  private readonly string p_folder;
  private readonly TimeSpan p_ttl;

  public FileCache(
    IReadOnlyLifetime _lifetime,
    string _folder,
    TimeSpan _ttl,
    long _maxFolderSize,
    TimeSpan _cleanUpInterval)
  {
    p_folder = _folder;
    p_ttl = _ttl;

    var scheduler = _lifetime.DisposeOnCompleted(new EventLoopScheduler());
    p_storeTasksFlow = _lifetime.DisposeOnCompleted(new Subject<FileCacheStoreTask>());
    p_getTasksFlow = _lifetime.DisposeOnCompleted(new Subject<FileCacheGetTask>());

    var schedulers = new IScheduler[] { scheduler };
    p_storeTeam = WorkerTeam.Run(p_storeTasksFlow, StoreInternalAsync, StorePenaltyAsync, _lifetime, schedulers);
    p_getTeam = WorkerTeam.Run<FileCacheGetTask, Stream>(p_getTasksFlow, GetInternalAsync, GetPenaltyAsync, _lifetime, schedulers);

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

        var enumerable = Directory
          .EnumerateFiles(p_folder, "*", SearchOption.AllDirectories)
          .Select(_ => new FileInfo(_))
          .OrderByDescending(_ => _.CreationTimeUtc);

        var folderSize = 0L;
        foreach (var file in enumerable)
        {
          if (now - file.LastWriteTimeUtc > _ttl && file.TryDelete())
            continue;

          folderSize += file.Length;
          if (folderSize > _maxFolderSize)
            file.TryDelete();
        }
      }, _lifetime);
  }

  public async Task StoreAsync(string _key, Stream _stream, CancellationToken _ct)
  {
    await p_storeTeam.DoWork(new FileCacheStoreTask(_stream, _key, _ct));
  }

  public async Task<Stream?> GetAsync(string _key, CancellationToken _ct)
  {
    return await p_getTeam.DoWork(new FileCacheGetTask(_key, _ct));
  }

  private async Task<bool> StoreInternalAsync(JobContext<FileCacheStoreTask, Unit> _ctx)
  {
    var job = _ctx.JobInfo.Job;
    if (!job.Data.CanRead)
      throw new IOException("Can't read stream!");

    var folder = GetFolderForKey(job.Key, out var hash);
    if (!Directory.Exists(folder))
      Directory.CreateDirectory(folder);

    var file = Path.Combine(folder, hash);

    using (var fileStream = File.Open(file, FileMode.Create, FileAccess.Write, FileShare.None))
      await job.Data.CopyToAsync(fileStream, job.Token);

    return true;
  }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
  private async Task<PenaltyInfo> StorePenaltyAsync(JobFailContext<FileCacheStoreTask> _ctx)
  {
    if (_ctx.FailedCounter > 1)
      return new PenaltyInfo(false, null);

    return new PenaltyInfo(true, TimeSpan.FromMilliseconds(250));
  }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
  private async Task<Stream?> GetInternalAsync(JobContext<FileCacheGetTask, Stream> _ctx)
  {
    var job = _ctx.JobInfo.Job;
    var folder = GetFolderForKey(job.Key, out var hash);

    var file = new FileInfo(Path.Combine(folder, hash));
    if (!file.Exists)
      return null;

    var now = DateTimeOffset.UtcNow;
    if (now - file.LastWriteTimeUtc > p_ttl)
    {
      file.TryDelete();
      return null;
    }

    return file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
  }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
  private async Task<PenaltyInfo> GetPenaltyAsync(JobFailContext<FileCacheGetTask> _ctx)
  {
    if (_ctx.FailedCounter > 1)
      return new PenaltyInfo(false, TimeSpan.Zero);

    return new PenaltyInfo(true, TimeSpan.FromMilliseconds(250));
  }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

  private string GetFolderForKey(string _key, out string _hash)
  {
    _hash = Cryptography.CalculateSHAHash(_key, SharedTypes.Data.HashComplexity.Bit256);
    var folder = Path.Combine(p_folder, _hash[..2], _hash[2..4]);
    return folder;
  }

}
