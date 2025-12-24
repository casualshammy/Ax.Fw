using Ax.Fw.DependencyInjection;
using Ax.Fw.Log;
using Ax.Fw.SharedTypes.Interfaces;
using System.Reactive.Linq;

namespace Ax.Fw.App;

/// <summary>
/// Provides a base class for configuring, composing, and running an application with dependency injection, lifetime
/// management, and logging capabilities.
/// </summary>
/// <remarks><para> <see cref="AppBase"/> enables fluent configuration of application services, modules, and
/// logging, as well as management of application lifetime and configuration sources. It acts as the entry point for
/// setting up dependency injection, registering services and modules, and controlling application startup and shutdown
/// behavior. </para> <para> To create an instance, use the static <see cref="Create"/> method. After configuring
/// dependencies and modules, call <see cref="Run"/> to build and start the application, or <see cref="RunWaitAsync"/>
/// to run and wait for the application lifetime to complete. </para> <para> This class is not intended to be inherited.
/// </para></remarks>
public sealed class AppBase
{
  private readonly AppDependencyManager p_depMgr;

  private AppBase()
  {
    p_depMgr = AppDependencyManager.Create();

    var lifetime = new Lifetime();
    lifetime.InstallConsoleCtrlCHook();
    p_depMgr.AddSingleton<ILifetime>(lifetime);
    p_depMgr.AddSingleton<IReadOnlyLifetime>(lifetime);

    var log = lifetime.ToDisposeOnEnded(new GenericLog());
    p_depMgr.AddSingleton<ILog>(log);
  }

  internal AppDependencyManager DependencyManager => p_depMgr;

  /// <summary>
  /// Creates a new instance of the <see cref="AppBase"/> class.
  /// </summary>
  /// <returns>A new <see cref="AppBase"/> instance.</returns>
  public static AppBase Create() => new();

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

}
