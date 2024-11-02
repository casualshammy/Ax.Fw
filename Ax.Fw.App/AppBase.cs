using Ax.Fw.App.Data;
using Ax.Fw.DependencyInjection;
using Ax.Fw.Extensions;
using Ax.Fw.JsonStorages;
using Ax.Fw.Log;
using Ax.Fw.SharedTypes.Data.Log;
using Ax.Fw.SharedTypes.Interfaces;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Ax.Fw.App;

public class AppBase
{
  public static AppBase Create() => new();

  private readonly AppDependencyManager p_depMgr;

  private AppBase()
  {
    p_depMgr = AppDependencyManager.Create();

    var lifetime = new Lifetime();
    lifetime.InstallConsoleCtrlCHook();
    p_depMgr.AddSingleton<ILifetime>(lifetime);
    p_depMgr.AddSingleton<IReadOnlyLifetime>(lifetime);

    var log = lifetime.ToDisposeOnEnded(new GenericLog(null));
    p_depMgr.AddSingleton<ILog>(log);
  }

  /// <summary>
  /// Build dependencies and return
  /// </summary>
  public void Run() => p_depMgr.Build();

  /// <summary>
  /// Build dependencies and return when lifetime is completed
  /// </summary>
  public async Task RunWaitAsync(bool _waitLifetime = true, TimeSpan? _waitLifetimeTime = null)
  {
    p_depMgr.Build();

    var lifetime = p_depMgr.Locate<IReadOnlyLifetime>();

    try
    {
      await Task.Delay(Timeout.InfiniteTimeSpan, lifetime.Token);
    }
    catch (TaskCanceledException)
    {
      // ignore
    }

    if (_waitLifetime)
    {
      var deadLine = DateTimeOffset.UtcNow + (_waitLifetimeTime ?? TimeSpan.FromSeconds(5));
      try
      {
        await lifetime.OnEnd
          .TakeUntil(deadLine)
          .LastOrDefaultAsync();
      }
      catch { }
    }
  }

  // ============================
  // ==== DEPENDENCY MANAGER ====
  // ============================

  public AppBase AddSingleton<T>() where T : class, new()
  {
    p_depMgr.AddSingleton<T>();
    return this;
  }

  public AppBase AddSingleton<T>(T _instance) where T : class
  {
    p_depMgr.AddSingleton(_instance);
    return this;
  }

  public AppBase AddSingleton<T, TInterface>() where T : notnull, TInterface, new()
  {
    p_depMgr.AddSingleton<T, TInterface>();
    return this;
  }

  public AppBase AddSingleton<T, TInterface>(T _instance) where T : notnull, TInterface
  {
    p_depMgr.AddSingleton<T, TInterface>(_instance);
    return this;
  }

  public AppBase AddSingleton<TInterface>(Func<IAppDependencyCtx, TInterface> _factory) where TInterface : notnull
  {
    p_depMgr.AddSingleton(_factory);
    return this;
  }

  public AppBase AddModule<T>() where T : notnull, IAppModule<T>
  {
    p_depMgr.AddModule<T>();
    return this;
  }

  public AppBase AddModule<T, TInterface>()
    where T : notnull, TInterface, IAppModule<TInterface>
    where TInterface : notnull
  {
    p_depMgr.AddModule<T, TInterface>();
    return this;
  }

  public AppBase ActivateOnStart<T>()
  {
    p_depMgr.ActivateOnStart<T>();
    return this;
  }

  public AppBase ActivateOnStart(Action _action)
  {
    p_depMgr.ActivateOnStart(_action);
    return this;
  }

  public AppBase ActivateOnStart<T1>(Action<T1> _action)
  {
    p_depMgr.ActivateOnStart(_action);
    return this;
  }

  public AppBase ActivateOnStart<T1, T2>(Action<T1, T2> _action)
  {
    p_depMgr.ActivateOnStart(_action);
    return this;
  }

  public AppBase ActivateOnStart<T1, T2, T3>(Action<T1, T2, T3> _action)
  {
    p_depMgr.ActivateOnStart(_action);
    return this;
  }

  // ===========================
  // =========== LOG ===========
  // ===========================

  public AppBase UseConsoleLog()
  {
    var log = p_depMgr.Locate<ILog>() as GenericLog;
    if (log == null)
      throw new InvalidOperationException($"Can't get the instance of {typeof(GenericLog)}!");

    log.AttachConsoleLog();
    return this;
  }

  public AppBase UseFileLog(
    Func<string> _fileNameFactory,
    Action<Exception, IEnumerable<LogEntry>>? _onError = null,
    Action<HashSet<string>>? _filesWrittenCallback = null)
  {
    var log = p_depMgr.Locate<ILog>() as GenericLog;
    if (log == null)
      throw new InvalidOperationException($"Can't get the instance of {typeof(GenericLog)}!");

    log.AttachFileLog(_fileNameFactory, TimeSpan.FromSeconds(1), _onError, _filesWrittenCallback);
    return this;
  }

  public AppBase UseFileLogFromConf<T>(
    Func<T?, string?> _fileNameFactory,
    Action<Exception, IEnumerable<LogEntry>>? _onError = null,
    Action<HashSet<string>>? _filesWrittenCallback = null)
    where T : class
  {
    var lifetime = p_depMgr.Locate<IReadOnlyLifetime>();

    var log = p_depMgr.Locate<ILog>() as GenericLog;
    if (log == null)
      throw new InvalidOperationException($"Can't get the instance of {typeof(GenericLog)}!");

    var confFlow = p_depMgr.Locate<IObservableConfig<T?>>();
    if (confFlow == null)
      throw new InvalidOperationException($"Can't get the instance of {typeof(IObservableConfig<T?>)}!");

    var confProp = confFlow.ToProperty(lifetime, null);

    string? factory()
    {
      var conf = confProp.Value;
      var path = _fileNameFactory.Invoke(conf);
      return path;
    }

    log.AttachFileLog(factory, TimeSpan.FromSeconds(1), _onError, _filesWrittenCallback);
    return this;
  }

  public AppBase UseFileLogRotate(FileLogRotateDescription _desc)
  {
    p_depMgr
      .ActivateOnStart((IReadOnlyLifetime _lifetime) =>
      {
        _lifetime.ToDisposeOnEnding(FileLoggerCleaner.Create(
          _desc.Directory, _desc.Recursive, _desc.LogFilesPattern, _desc.LogFileTtl, _desc.GzipFiles, TimeSpan.FromHours(3)));
      });

    return this;
  }

  public AppBase UseFileLogRotateFromConf<T>(
    Func<T?, FileLogRotateDescription?> _factory)
  {
    p_depMgr
      .ActivateOnStart((IObservableConfig<T?> _confFlow, IReadOnlyLifetime _lifetime) =>
      {
        var scheduler = new EventLoopScheduler();

        _confFlow
          .HotAlive(_lifetime, scheduler, (_conf, _life) =>
          {
            var desc = _factory.Invoke(_conf);
            if (desc == null)
              return;

            _life.ToDisposeOnEnding(FileLoggerCleaner.Create(
              desc.Directory, desc.Recursive, desc.LogFilesPattern, desc.LogFileTtl, desc.GzipFiles, TimeSpan.FromHours(3)));
          });
      });

    return this;
  }

  // ===========================
  // ========= CONFIGS =========
  // ===========================

  public AppBase UseConfigFile<TRaw, TOut>(
    string _filePath,
    JsonSerializerContext? _jsonCtx,
    Func<IAppDependencyCtx, TRaw?, TOut?> _transform)
    where TRaw : class
    where TOut : class
  {
    p_depMgr.AddSingleton(_ctx =>
    {
      return _ctx.CreateInstance<IReadOnlyLifetime, IObservableConfig<TOut?>>((IReadOnlyLifetime _lifetime) =>
      {
        void tryLogDeserializationError(Exception _ex)
        {
          var log = _ctx.LocateOrDefault<ILog>();
          log?.Warn($"Can't parse config file '{_filePath}': {_ex.Message}");
        }

        var observable = new JsonStorage<TRaw>(_filePath, _jsonCtx, _lifetime, tryLogDeserializationError)
          .Select(_ => _transform(_ctx, _));

        return new ObservableConfig<TOut>(observable);
      });
    });

    return this;
  }

  public AppBase UseConfigFile<T>(string _filePath, JsonSerializerContext? _jsonCtx)
    where T : class
  {
    return UseConfigFile<T, T>(_filePath, _jsonCtx, (_, _raw) => _raw);
  }

  public AppBase UseConfigFile<T>()
    where T : class, IConfigDefinition
  {
    var filePath = T.FilePath;
    var jsonCtx = T.JsonCtx;

    return UseConfigFile<T>(filePath, jsonCtx);
  }

}
