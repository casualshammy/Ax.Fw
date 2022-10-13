using System;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface IImportClassAggregator
{
    IServiceProvider ServiceProvider { get; }

    T Locate<T>() where T : notnull;
    T? LocateOrNull<T>() where T : notnull;
}