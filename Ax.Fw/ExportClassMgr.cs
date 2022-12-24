using Ax.Fw.Attributes;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Grace.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Ax.Fw;

public class ExportClassMgr : IExportClassMgr
{
    private readonly List<object> p_instances = new();
    private readonly DependencyInjectionContainer p_container = new();

    public ExportClassMgr(
        IReadOnlyLifetime _lifetime, 
        IReadOnlyDictionary<Type, Func<IExportLocatorScope, object>>? _singletoneInstances = null, 
        IEnumerable<Assembly>? _loadReferencedAssembliesFrom = null)
    {
        if (_loadReferencedAssembliesFrom != null)
        {
            foreach (var assembly in _loadReferencedAssembliesFrom.SelectMany(_x => _x.GetReferencedAssemblies()).DistinctBy(_x => _x.FullName))
            {
                Assembly.Load(assembly);
                Debug.WriteLine($"{nameof(ExportClassMgr)}: Loaded assembly: {assembly?.FullName}");
            }
        }

        if (_singletoneInstances is not null)
            foreach (var pair in _singletoneInstances)
                p_container.Configure(_x => _x.ExportFuncWithContext((_scope, _, _) => pair.Value(_scope)).As(pair.Key).Lifestyle.Singleton());

        var exportTypes = Utilities.GetTypesWithAttr<ExportClassAttribute>(true);
        foreach (var type in exportTypes)
        {
            if (Attribute.GetCustomAttribute(type, typeof(ExportClassAttribute)) is not ExportClassAttribute exportInfo)
                continue;

            if (exportInfo.Singleton)
                p_container.Configure(_x => _x.Export(type).As(exportInfo.InterfaceType).Lifestyle.Singleton());
            else
                p_container.Configure(_x => _x.Export(type).As(exportInfo.InterfaceType));
        }

        foreach (var type in exportTypes)
        {
            if (Attribute.GetCustomAttribute(type, typeof(ExportClassAttribute)) is not ExportClassAttribute exportInfo)
                continue;

            if (exportInfo.ActivateOnStart && exportInfo.Singleton)
            {
                var instance = p_container.Locate(exportInfo.InterfaceType);
                p_instances.Add(instance);
                if (instance is IDisposable disposable)
                    _lifetime.DisposeOnCompleted(disposable);
            }
        }
    }

    public IInjectionScope ServiceProvider => p_container;

    public T Locate<T>() where T : notnull => p_container.Locate<T>();

    public T? LocateOrNull<T>() where T : notnull => p_container.LocateOrDefault<T>();

}
