using Grace.DependencyInjection;
using Grace.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Ax.Fw.Extensions.Parts;

internal class GraceSrvProviderFactory : IServiceProviderFactory<IInjectionScope>
{
    private readonly IInjectionScope p_child;

    public GraceSrvProviderFactory(IInjectionScope _child)
    {
        p_child = _child;
    }

    public IInjectionScope CreateBuilder(IServiceCollection _services)
    {
        IInjectionScope injectionScope = p_child.CreateChildScope();
        injectionScope.Populate(_services);
        return injectionScope;
    }

    public IServiceProvider CreateServiceProvider(IInjectionScope _containerBuilder)
    {
        return _containerBuilder.Locate<IServiceProvider>();
    }
}
