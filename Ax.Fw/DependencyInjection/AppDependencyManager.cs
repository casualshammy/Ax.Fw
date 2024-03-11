using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Ax.Fw.DependencyInjection;

public sealed class AppDependencyManager : IReadOnlyDependencyContainer
{
  private readonly IAppDependencyCtx p_ctx;
  private readonly ConcurrentDictionary<Type, Lazy<object>> p_dependencies = [];
  private ImmutableList<Action> p_actions = [];
  private volatile int p_built = 0;

  private AppDependencyManager()
  {
    p_ctx = new AppDependencyCtx(this);
  }

  public static AppDependencyManager Create() => new();

  public AppDependencyManager AddSingleton<T>() where T : class, new()
  {
    p_dependencies.AddOrUpdate(
      typeof(T),
      new Lazy<object>(() => new T(), LazyThreadSafetyMode.ExecutionAndPublication),
      (_type, _obj) => throw new InvalidOperationException($"Instance of type '{typeof(T).Name}' is already registered"));
    return this;
  }

  public AppDependencyManager AddSingleton<T>(T _instance) where T : class
  {
    p_dependencies.AddOrUpdate(
      typeof(T),
      new Lazy<object>(() => _instance, LazyThreadSafetyMode.ExecutionAndPublication),
      (_type, _obj) => throw new InvalidOperationException($"Instance of type '{typeof(T).Name}' is already registered"));
    return this;
  }

  public AppDependencyManager AddSingleton<T, TInterface>() where T : notnull, TInterface, new()
  {
    p_dependencies.AddOrUpdate(
      typeof(TInterface),
      new Lazy<object>(() => new T(), LazyThreadSafetyMode.ExecutionAndPublication),
      (_type, _obj) => throw new InvalidOperationException($"Instance of type '{typeof(T).Name}' is already registered"));
    return this;
  }

  public AppDependencyManager AddSingleton<T, TInterface>(T _instance) where T : notnull, TInterface
  {
    p_dependencies.AddOrUpdate(
      typeof(TInterface),
      new Lazy<object>(() => _instance, LazyThreadSafetyMode.ExecutionAndPublication),
      (_type, _obj) => throw new InvalidOperationException($"Instance of type '{typeof(T).Name}' is already registered"));
    return this;
  }

  public AppDependencyManager AddSingleton<TInterface>(Func<IAppDependencyCtx, TInterface> _factory) where TInterface : notnull
  {
    p_dependencies.AddOrUpdate(
      typeof(TInterface),
      new Lazy<object>(() => _factory(p_ctx), LazyThreadSafetyMode.ExecutionAndPublication),
      (_type, _obj) => throw new InvalidOperationException($"Instance of type '{typeof(TInterface).Name}' is already registered"));

    return this;
  }

  public AppDependencyManager AddModule<T>() where T : notnull, IAppModule<T>
  {
    p_dependencies.AddOrUpdate(
      typeof(T),
      new Lazy<object>(() => T.ExportInstance(p_ctx), LazyThreadSafetyMode.ExecutionAndPublication),
      (_type, _obj) => throw new InvalidOperationException($"Instance of type '{typeof(T).Name}' is already registered"));

    return this;
  }

  public AppDependencyManager AddModule<TImpl, TInterface>()
    where TImpl : notnull, TInterface, IAppModule<TInterface>
    where TInterface : notnull
  {
    p_dependencies.AddOrUpdate(
      typeof(TInterface),
      new Lazy<object>(() => TImpl.ExportInstance(p_ctx), LazyThreadSafetyMode.ExecutionAndPublication),
      (_type, _obj) => throw new InvalidOperationException($"Instance of type '{typeof(TImpl).Name}' is already registered"));

    return this;
  }

  public AppDependencyManager ActivateOnStart<T>()
  {
    p_actions = p_actions.Add(() =>
    {
      var type = typeof(T);
      if (!p_dependencies.TryGetValue(type, out var instanceFactory))
        throw new KeyNotFoundException($"Instance of type '{type.Name}' is not found!");

      _ = instanceFactory.Value;
    });

    return this;
  }

  public AppDependencyManager ActivateOnStart(Action _action)
  {
    p_actions = p_actions.Add(_action);
    return this;
  }

  public AppDependencyManager ActivateOnStart<T1>(Action<T1> _action)
  {
    p_actions = p_actions.Add(() =>
    {
      var t1 = Locate<T1>();
      _action(t1);
    });

    return this;
  }

  public AppDependencyManager ActivateOnStart<T1, T2>(Action<T1, T2> _action)
  {
    p_actions = p_actions.Add(() =>
    {
      var t1 = Locate<T1>();
      var t2 = Locate<T2>();
      _action(t1, t2);
    });

    return this;
  }

  public AppDependencyManager ActivateOnStart<T1, T2, T3>(Action<T1, T2, T3> _action)
  {
    p_actions = p_actions.Add(() =>
    {
      var t1 = Locate<T1>();
      var t2 = Locate<T2>();
      var t3 = Locate<T3>();
      _action(t1, t2, t3);
    });

    return this;
  }

  public T Locate<T>()
  {
    if (!p_dependencies.TryGetValue(typeof(T), out var instanceFactory))
      throw new KeyNotFoundException($"Instance of type '{typeof(T).Name}' is not found!");

    return (T)instanceFactory.Value;
  }

  public T? LocateOrDefault<T>()
  {
    if (!p_dependencies.TryGetValue(typeof(T), out var instanceFactory))
      return default;

    return (T)instanceFactory.Value;
  }

  public AppDependencyManager Build()
  {
    if (Interlocked.Exchange(ref p_built, 1) == 1)
      throw new InvalidOperationException($"This instance of '{typeof(AppDependencyManager).Name}' is already built");

    foreach (var action in p_actions)
      action();

    p_actions = [];

    return this;
  }

}
