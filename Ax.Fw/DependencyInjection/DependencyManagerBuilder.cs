using Ax.Fw.Attributes;
using Ax.Fw.SharedTypes.Interfaces;
using Grace.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ax.Fw.DependencyInjection;

public class DependencyManagerBuilder
{
  private readonly IReadOnlyLifetime p_lifetime;
  private readonly Assembly p_mainAssembly;
  private readonly DependencyInjectionContainer p_container = new();

  private DependencyManagerBuilder(IReadOnlyLifetime _lifetime, Assembly _mainAssembly)
  {
    p_lifetime = _lifetime;
    p_mainAssembly = _mainAssembly;
  }

  public static DependencyManagerBuilder Create(IReadOnlyLifetime _lifetime, Assembly _mainAssembly)
  {
    return new DependencyManagerBuilder(_lifetime, _mainAssembly);
  }

  public DependencyManagerBuilder AddSingleton<T>(T _instance) where T : notnull
  {
    p_container.Configure(_ => _.ExportInstance(_instance));
    return this;
  }

  public DependencyManagerBuilder AddSingleton<T>(Func<IExportLocatorScope, T> _factory) where T : notnull
  {
    p_container.Configure(_ => _.ExportFactory(() => _factory(_.OwningScope)).Lifestyle.Singleton());
    return this;
  }

  public DependencyManagerBuilder AddSingleton(Type _type, Func<IExportLocatorScope, object> _factory)
  {
    p_container.Configure(_ => _.ExportFactory(() => _factory(_.OwningScope)).As(_type).Lifestyle.Singleton());
    return this;
  }

  public DependencyManagerBuilder AddTransient<T>(Func<IExportLocatorScope, T> _factory)
  {
    p_container.Configure(_ => _.ExportFactory(() => _factory(_.OwningScope)));
    return this;
  }

  public DependencyManagerBuilder AddTransient(Type _type, Func<IExportLocatorScope, object> _factory)
  {
    p_container.Configure(_ => _.ExportFactory(() => _factory(_.OwningScope)).As(_type));
    return this;
  }

  public DependencyManager Build()
  {
    var exportClassAttrType = typeof(ExportClassAttribute);
    var exportTypes = p_mainAssembly
      .GetTypes()
      .Where(_t => _t.IsDefined(exportClassAttrType, false))
      .ToList();

    foreach (var type in exportTypes)
    {
      if (Attribute.GetCustomAttribute(type, exportClassAttrType) is not ExportClassAttribute exportInfo)
        continue;

      if (exportInfo.Singleton)
        p_container.Configure(_x => _x.Export(type).As(exportInfo.InterfaceType).Lifestyle.Singleton());
      else
        p_container.Configure(_x => _x.Export(type).As(exportInfo.InterfaceType));
    }

    var instances = new List<object>();
    foreach (var type in exportTypes)
    {
      if (Attribute.GetCustomAttribute(type, exportClassAttrType) is not ExportClassAttribute exportInfo)
        continue;

      if (exportInfo.ActivateOnStart && exportInfo.Singleton)
      {
        var instance = p_container.Locate(exportInfo.InterfaceType);
        instances.Add(instance);
        if (instance is IDisposable disposable)
          p_lifetime.ToDisposeOnEnding(disposable);
      }
    }

    return new(p_container, instances);
  }

}