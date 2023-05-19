using Ax.Fw.SharedTypes.Interfaces;
using Grace.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Ax.Fw.DependencyInjection;

public class DependencyManagerBuilder
{
  private readonly IReadOnlyLifetime p_lifetime;
  private readonly Assembly? p_scanOnlyThisAssembly;
  private readonly ConcurrentDictionary<Type, Func<IExportLocatorScope, object>> p_singletons = new();
  private readonly List<Assembly> p_assemlies = new();

  private DependencyManagerBuilder(IReadOnlyLifetime _lifetime, Assembly? _scanOnlyThisAssembly)
  {
    p_lifetime = _lifetime;
    p_scanOnlyThisAssembly = _scanOnlyThisAssembly;
  }

  public static DependencyManagerBuilder Create(IReadOnlyLifetime _lifetime, Assembly? _scanOnlyThisAssembly = null)
  {
    return new DependencyManagerBuilder(_lifetime, _scanOnlyThisAssembly);
  }

  public DependencyManagerBuilder AddSingleton<T>(T _instance) where T : notnull
  {
    p_singletons[typeof(T)] = _ => _instance;
    return this;
  }

  public DependencyManagerBuilder AddSingleton<T>(Func<IExportLocatorScope, object> _factory) where T : notnull
  {
    p_singletons[typeof(T)] = _factory;
    return this;
  }

  public DependencyManagerBuilder AddSingleton(Type _type, Func<IExportLocatorScope, object> _factory)
  {
    p_singletons[_type] = _factory;
    return this;
  }

  public DependencyManagerBuilder AddAssemblyReference(Assembly _assembly)
  {
    p_assemlies.Add(_assembly);
    return this;
  }

  public DependencyManagerBuilder AddAssembliesReference(IEnumerable<Assembly> _assemblies)
  {
    p_assemlies.AddRange(_assemblies);
    return this;
  }

  public DependencyManager Build() => new(p_lifetime, p_singletons, p_assemlies, p_scanOnlyThisAssembly);

}