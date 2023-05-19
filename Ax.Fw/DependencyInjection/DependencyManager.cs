using Ax.Fw.Attributes;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Grace.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Ax.Fw.DependencyInjection;

public class DependencyManager : IDependencyManager
{
  private readonly List<object> p_instances = new();
  private readonly DependencyInjectionContainer p_container = new();

  internal DependencyManager(
    IReadOnlyLifetime _lifetime,
    IReadOnlyDictionary<Type, Func<IExportLocatorScope, object>> _singletoneInstances,
    IEnumerable<Assembly> _loadReferencedAssembliesFrom,
    Assembly? _scanOnlyThisAssembly)
  {
    foreach (var assembly in _loadReferencedAssembliesFrom.SelectMany(_x => _x.GetReferencedAssemblies()).DistinctBy(_x => _x.FullName))
    {
      Assembly.Load(assembly);
      Debug.WriteLine($"{nameof(DependencyManager)}: Loaded assembly: {assembly?.FullName}");
    }

    foreach (var pair in _singletoneInstances)
      p_container.Configure(_x => _x.ExportFuncWithContext((_scope, _, _) => pair.Value(_scope)).As(pair.Key).Lifestyle.Singleton());

    var exportClassAttrType = typeof(ExportClassAttribute);
    Type[] exportTypes;

    if (_scanOnlyThisAssembly != null)
    {
      exportTypes = _scanOnlyThisAssembly
        .GetTypes()
        .Where(_t => _t.IsDefined(exportClassAttrType, false))
        .ToArray();
    }
    else
      exportTypes = Utilities
          .GetTypesWithAttr<ExportClassAttribute>(true)
          .ToArray();

    foreach (var type in exportTypes)
    {
      if (Attribute.GetCustomAttribute(type, exportClassAttrType) is not ExportClassAttribute exportInfo)
        continue;

      if (exportInfo.Singleton)
        p_container.Configure(_x => _x.Export(type).As(exportInfo.InterfaceType).Lifestyle.Singleton());
      else
        p_container.Configure(_x => _x.Export(type).As(exportInfo.InterfaceType));
    }

    foreach (var type in exportTypes)
    {
      if (Attribute.GetCustomAttribute(type, exportClassAttrType) is not ExportClassAttribute exportInfo)
        continue;

      if (exportInfo.ActivateOnStart && exportInfo.Singleton)
      {
        var instance = p_container.Locate(exportInfo.InterfaceType);
        p_instances.Add(instance);
        if (instance is IDisposable disposable)
          _lifetime.DisposeOnCompleted(disposable);
      }
    }

    ServiceProvider = p_container;
  }

  public IInjectionScope ServiceProvider { get; }

  public T Locate<T>() where T : notnull => p_container.Locate<T>();

  public T? LocateOrDefault<T>() where T : notnull => p_container.LocateOrDefault<T>();

}
