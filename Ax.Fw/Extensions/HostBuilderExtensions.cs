using Ax.Fw.Extensions.Parts;
using Grace.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ax.Fw.Extensions;

public static class HostBuilderExtensions
{
    public static IHostBuilder UseGraceContainer(this IHostBuilder _this, IInjectionScope _container)
    {
        return _this.UseServiceProviderFactory(new GraceSrvProviderFactory(_container));
    }
}
