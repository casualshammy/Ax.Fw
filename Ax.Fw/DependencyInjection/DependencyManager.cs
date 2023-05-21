using Ax.Fw.SharedTypes.Interfaces;
using Grace.DependencyInjection;
using System.Collections.Generic;

namespace Ax.Fw.DependencyInjection;

public class DependencyManager : IDependencyManager
{
  private readonly IReadOnlyList<object> p_instances;

  internal DependencyManager(
    IInjectionScope _injectionScope, 
    IReadOnlyList<object> _instances)
  {
    ServiceProvider = _injectionScope;
    p_instances = _instances;
  }

  public IInjectionScope ServiceProvider { get; }

  public T Locate<T>() where T : notnull => ServiceProvider.Locate<T>();

  public T? LocateOrDefault<T>() where T : notnull => ServiceProvider.LocateOrDefault<T>();

}
