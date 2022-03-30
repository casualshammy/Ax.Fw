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

namespace Ax.Fw.ClassExport
{
    public class AutoActivator
    {
        private readonly List<object> p_instances = new();

        public AutoActivator(ILifetime _lifetime, ServiceCollection _serviceCollection, IEnumerable<Assembly>? _loadReferencedAssembliesFrom = null)
        {
            if (_loadReferencedAssembliesFrom != null)
            {
                foreach (var assembly in _loadReferencedAssembliesFrom.SelectMany(_x => _x.GetReferencedAssemblies()).DistinctBy(_x => _x.FullName))
                {
                    Assembly.Load(assembly);
                    Debug.WriteLine($"{nameof(AutoActivator)}: Loaded assembly: {assembly?.FullName}");
                }
            }

            foreach (var type in Utilities.GetTypesWith<AutoActivatorExportAttribute>(true))
            {
                var exportInfo = (AutoActivatorExportAttribute)Attribute.GetCustomAttribute(type, typeof(AutoActivatorExportAttribute));
                if (exportInfo.Singletone)
                    _serviceCollection = (ServiceCollection)_serviceCollection.AddSingleton(exportInfo.InterfaceType, type);
                else
                    _serviceCollection = (ServiceCollection)_serviceCollection.AddTransient(exportInfo.InterfaceType, type);
            }
            foreach (var type in Utilities.GetTypesWith<ImportClassAttribute>(true))
            {
                var exportInfo = (ImportClassAttribute)Attribute.GetCustomAttribute(type, typeof(ImportClassAttribute));
                if (exportInfo.Singletone)
                    _serviceCollection = (ServiceCollection)_serviceCollection.AddSingleton(exportInfo.InterfaceType, type);
                else
                    _serviceCollection = (ServiceCollection)_serviceCollection.AddTransient(exportInfo.InterfaceType, type);
            }
            ServiceProvider = _serviceCollection.BuildServiceProvider();
            foreach (var type in Utilities.GetTypesWith<AutoActivatorExportAttribute>(true))
            {
                var exportInfo = (AutoActivatorExportAttribute)Attribute.GetCustomAttribute(type, typeof(AutoActivatorExportAttribute));
                if (exportInfo.ActivateOnStart && exportInfo.Singletone)
                {
                    var instance = ServiceProvider.GetRequiredService(exportInfo.InterfaceType);
                    p_instances.Add(instance);
                    if (exportInfo.DisposeRequired)
                        _lifetime.DisposeOnCompleted(instance as IDisposable);
                }
            }
            foreach (var type in Utilities.GetTypesWith<ImportClassAttribute>(true))
            {
                var exportInfo = (ImportClassAttribute)Attribute.GetCustomAttribute(type, typeof(ImportClassAttribute));
                if (exportInfo.ActivateOnStart && exportInfo.Singletone)
                {
                    var instance = ServiceProvider.GetRequiredService(exportInfo.InterfaceType);
                    p_instances.Add(instance);
                    if (exportInfo.DisposeRequired)
                        _lifetime.DisposeOnCompleted(instance as IDisposable);
                }
            }
        }

        public IServiceProvider ServiceProvider { get; }

    }

}
