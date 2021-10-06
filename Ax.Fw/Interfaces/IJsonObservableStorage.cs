#nullable enable
using System;

namespace Ax.Fw.Interfaces
{
    public interface IJsonObservableStorage<T>
    {
        IObservable<T?> Changes { get; }
    }
}