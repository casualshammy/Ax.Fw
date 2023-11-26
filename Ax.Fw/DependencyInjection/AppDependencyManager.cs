﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Ax.Fw.DependencyInjection;

#if NET8_0_OR_GREATER

public sealed class AppDependencyManager
{
  private readonly IAppDependencyCtx p_ctx;
  private readonly ConcurrentDictionary<Type, Lazy<object>> p_dependencies = [];
  private ImmutableHashSet<Type> p_activateOnStart = [];
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
      new Lazy<object>(() => new T(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication),
      (_type, _obj) => throw new InvalidOperationException($"Instance of type '{typeof(T).Name}' is already registered"));
    return this;
  }

  public AppDependencyManager AddSingleton<T>(T _instance) where T : class
  {
    p_dependencies.AddOrUpdate(
      typeof(T),
      new Lazy<object>(() => _instance, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication),
      (_type, _obj) => throw new InvalidOperationException($"Instance of type '{typeof(T).Name}' is already registered"));
    return this;
  }

  public AppDependencyManager AddSingleton<T, TInterface>() where T : notnull, TInterface, new()
  {
    p_dependencies.AddOrUpdate(
      typeof(TInterface),
      new Lazy<object>(() => new T(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication),
      (_type, _obj) => throw new InvalidOperationException($"Instance of type '{typeof(T).Name}' is already registered"));
    return this;
  }

  public AppDependencyManager AddSingleton<T, TInterface>(T _instance) where T : notnull, TInterface
  {
    p_dependencies.AddOrUpdate(
      typeof(TInterface),
      new Lazy<object>(() => _instance, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication),
      (_type, _obj) => throw new InvalidOperationException($"Instance of type '{typeof(T).Name}' is already registered"));
    return this;
  }

  public AppDependencyManager AddModule<T>() where T : notnull, IAppModule<T>
  {
    p_dependencies.AddOrUpdate(
      typeof(T),
      new Lazy<object>(() => T.ExportInstance(p_ctx), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication),
      (_type, _obj) => throw new InvalidOperationException($"Instance of type '{typeof(T).Name}' is already registered"));
    return this;
  }

  public AppDependencyManager AddModule<T, TInterface>() where T : notnull, TInterface, IAppModule<T>
  {
    p_dependencies.AddOrUpdate(
      typeof(TInterface),
      new Lazy<object>(() => T.ExportInstance(p_ctx), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication),
      (_type, _obj) => throw new InvalidOperationException($"Instance of type '{typeof(T).Name}' is already registered"));
    return this;
  }

  public AppDependencyManager ActivateOnStart<T>()
  {
    p_activateOnStart = p_activateOnStart.Add(typeof(T));
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

  public AppDependencyManager Run()
  {
    if (Interlocked.Exchange(ref p_built, 1) == 1)
      throw new InvalidOperationException($"This instance of '{typeof(AppDependencyManager).Name}' is already built");

    foreach (var type in p_activateOnStart)
    {
      if (!p_dependencies.TryGetValue(type, out var instanceFactory))
        throw new KeyNotFoundException($"Instance of type '{type.Name}' is not found!");

      _ = instanceFactory.Value;
    }

    p_activateOnStart = [];

    return this;
  }

}

#endif