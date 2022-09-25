#nullable enable

using Ax.Fw.Attributes;
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Ax.Fw;

public class ImportClassAggregator
{
    private readonly List<object> p_instances = new();

    public ImportClassAggregator(ILifetime _lifetime, IServiceCollection _serviceCollection, IEnumerable<Assembly>? _loadReferencedAssembliesFrom = null)
    {
        if (_loadReferencedAssembliesFrom != null)
        {
            foreach (var assembly in _loadReferencedAssembliesFrom.SelectMany(_x => _x.GetReferencedAssemblies()).DistinctBy(_x => _x.FullName))
            {
                Assembly.Load(assembly);
                Debug.WriteLine($"{nameof(ImportClassAggregator)}: Loaded assembly: {assembly?.FullName}");
            }
        }

        var types = Utilities.GetTypesWith<ImportClassAttribute>(true);

        foreach (var type in types)
        {
            var exportInfo = (ImportClassAttribute)Attribute.GetCustomAttribute(type, typeof(ImportClassAttribute));
            if (exportInfo.Singleton)
                _serviceCollection = _serviceCollection.AddSingleton(exportInfo.InterfaceType, type);
            else
                _serviceCollection = _serviceCollection.AddTransient(exportInfo.InterfaceType, type);
        }

        ServiceProvider = _serviceCollection.BuildServiceProvider();
        Services = _serviceCollection;

        foreach (var type in types)
        {
            var exportInfo = (ImportClassAttribute)Attribute.GetCustomAttribute(type, typeof(ImportClassAttribute));
            if (exportInfo.ActivateOnStart && exportInfo.Singleton)
            {
                var instance = ServiceProvider.GetRequiredService(exportInfo.InterfaceType);
                p_instances.Add(instance);
                if (exportInfo.DisposeRequired)
                    _lifetime.DisposeOnCompleted(instance as IDisposable);
            }
        }
    }

    public IServiceProvider ServiceProvider { get; }
    public IServiceCollection Services { get; }

    public T Locate<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();

    public T? LocateOrNull<T>() where T : notnull => ServiceProvider.GetService<T>();

}
