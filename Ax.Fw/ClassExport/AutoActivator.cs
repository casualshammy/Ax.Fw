#nullable enable
using Ax.Fw.Attributes;
using Ax.Fw.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Ax.Fw.ClassExport
{
    public class AutoActivator
    {
        private readonly List<object> p_instances = new();

        public AutoActivator(ILifetime _lifetime, ServiceCollection _serviceCollection)
        {
            foreach (var type in Utilities.GetTypesWith<AutoActivatorExportAttribute>(true))
            {
                var exportInfo = (AutoActivatorExportAttribute)Attribute.GetCustomAttribute(type, typeof(AutoActivatorExportAttribute));
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
        }

        public IServiceProvider ServiceProvider { get; }

    }

}
