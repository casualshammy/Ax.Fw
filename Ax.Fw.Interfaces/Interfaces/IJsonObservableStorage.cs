#nullable enable
using System;

namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface IJsonObservableStorage<T>
    {
        IObservable<T?> Changes { get; }
    }
}