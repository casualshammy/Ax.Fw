using Ax.Fw.DependencyInjection;
using Ax.Fw.Log;
using Ax.Fw.SharedTypes.Interfaces;
using System.Text.RegularExpressions;

namespace Ax.Fw.App;

public class AppBase
{
  public static AppBase Create() => new();

  private readonly AppDependencyManager p_depMgr;

  private AppBase()
  {
    p_depMgr = AppDependencyManager.Create();
  }

  public void Run()
  {
    var lifetime = new Lifetime();
    lifetime.InstallConsoleCtrlCHook();

    p_depMgr.AddSingleton<ILifetime>(lifetime);
    p_depMgr.AddSingleton<IReadOnlyLifetime>(lifetime);

    p_depMgr.Build();
  }

  public async Task RunWaitAsync()
  {
    var lifetime = new Lifetime();
    lifetime.InstallConsoleCtrlCHook();

    p_depMgr.AddSingleton<ILifetime>(lifetime);
    p_depMgr.AddSingleton<IReadOnlyLifetime>(lifetime);

    p_depMgr.Build();

    try
    {
      await Task.Delay(Timeout.InfiniteTimeSpan, lifetime.Token);
    }
    catch (TaskCanceledException)
    {
      // ignore
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

  public AppBase AddModule<T, TInterface>() where T : notnull, TInterface, IAppModule<T>
  {
    p_depMgr.AddModule<T, TInterface>();
    return this;
  }

  public AppBase ActivateOnStart<T>()
  {
    p_depMgr.ActivateOnStart<T>();
    return this;
  }

  // ===========================
  // =========== LOG ===========
  // ===========================

  public AppBase UseFileAndConsoleLog(Func<string> _fileNameFactory)
  {
    p_depMgr.AddSingleton<ILogger>(_ctx =>
    {
      return _ctx.CreateInstance((IReadOnlyLifetime _lifetime) =>
      {
        var fileLog = _lifetime.ToDisposeOnEnded(new FileLogger(_fileNameFactory, TimeSpan.FromSeconds(1)));
        var consoleLog = _lifetime.ToDisposeOnEnded(new ConsoleLogger());
        return new CompositeLogger(fileLog, consoleLog);
      });
    });

    return this;
  }

  public AppBase UseFileLogRotate(DirectoryInfo _logFolder, bool _recursive, Regex _logFilesPattern, TimeSpan _logFileTtl)
  {
    p_depMgr
      .AddSingleton(_ctx =>
      {
        return _ctx.CreateInstance(() =>
        {
          var fileLogRotate = FileLoggerCleaner.Create(_logFolder, _recursive, _logFilesPattern, _logFileTtl, TimeSpan.FromHours(3));
          return fileLogRotate;
        });
      })
      .ActivateOnStart<FileLoggerCleaner>();

    return this;
  }

  public AppBase UseConsoleLog()
  {
    p_depMgr.AddSingleton<ILogger>(_ctx =>
    {
      return _ctx.CreateInstance((IReadOnlyLifetime _lifetime) => _lifetime.ToDisposeOnEnded(new ConsoleLogger()));
    });

    return this;
  }

}
