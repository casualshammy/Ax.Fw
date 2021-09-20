using Ax.Fw.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ax.Fw.SingletoneExport
{
    public class SingletoneActivator
    {
        private readonly List<object> p_instances = new();

        public SingletoneActivator(ILifetime _lifetime, ServiceCollection _serviceCollection)
        {
            foreach (var singletone in GetTypesWith<SingletoneActivatorAutoExportAttribute>(true))
            {
                var autoExportInfo = (SingletoneActivatorAutoExportAttribute)Attribute.GetCustomAttribute(singletone, typeof(SingletoneActivatorAutoExportAttribute));
                _serviceCollection = (ServiceCollection)_serviceCollection.AddSingleton(autoExportInfo.InterfaceType, singletone);
            }
            ServiceProvider = _serviceCollection.BuildServiceProvider();
            foreach (var singletone in GetTypesWith<SingletoneActivatorAutoExportAttribute>(true))
            {
                var autoExportInfo = (SingletoneActivatorAutoExportAttribute)Attribute.GetCustomAttribute(singletone, typeof(SingletoneActivatorAutoExportAttribute));
                if (autoExportInfo.ActivateOnStart)
                {
                    var instance = ServiceProvider.GetRequiredService(autoExportInfo.InterfaceType);
                    p_instances.Add(instance);
                    if (autoExportInfo.DisposeRequired)
                        _lifetime.DisposeOnCompleted(instance as IDisposable);
                }
            }
        }

        public IServiceProvider ServiceProvider { get; }

        private static IEnumerable<Type> GetTypesWith<TAttribute>(bool inherit) where TAttribute : Attribute
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => x.IsDefined(typeof(TAttribute), inherit));
        }
    }

}
