using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ax.Fw.SingletoneExport
{
    public class SingletoneActivator
    {
        private readonly List<object> p_instances = new List<object>();

        public SingletoneActivator(ServiceCollection _serviceCollection)
        {
            foreach (var singletone in GetTypesWith<SingletoneActivatorAutoExportAttribute>(true))
            {
                var autoExportInfo = (SingletoneActivatorAutoExportAttribute)Attribute.GetCustomAttribute(singletone, typeof(SingletoneActivatorAutoExportAttribute));
                if (autoExportInfo.Singleton)
                    _serviceCollection = (ServiceCollection)_serviceCollection.AddSingleton(autoExportInfo.InterfaceType, singletone);
            }
            ServiceProvider = _serviceCollection.BuildServiceProvider();
            foreach (var singletone in GetTypesWith<SingletoneActivatorAutoExportAttribute>(true))
            {
                var autoExportInfo = (SingletoneActivatorAutoExportAttribute)Attribute.GetCustomAttribute(singletone, typeof(SingletoneActivatorAutoExportAttribute));
                if (autoExportInfo.Singleton && autoExportInfo.ActivateOnStart)
                    p_instances.Add(ServiceProvider.GetRequiredService(autoExportInfo.InterfaceType));
            }
        }

        public IServiceProvider ServiceProvider { get; }

        private static IEnumerable<Type> GetTypesWith<TAttribute>(bool inherit) where TAttribute : Attribute
        {
            return from a in AppDomain.CurrentDomain.GetAssemblies()
                   from t in a.GetTypes()
                   where t.IsDefined(typeof(TAttribute), inherit)
                   select t;
        }
    }

}
