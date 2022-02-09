#nullable enable
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using System;

namespace Ax.Fw
{
    /// <summary>
    /// This class can subscribe to IObservable and store its last value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RxProperty<T> : IRxProperty<T>
    {
        public RxProperty(IObservable<T?> _observable, IReadOnlyLifetime _lifetime, T? _defaultValue = default)
        {
            Value = _defaultValue;
            Observable = _observable;
            _observable
                .Subscribe(_x => Value = _x, _lifetime);
        }

        public T? Value { get; private set; }
        public IObservable<T?> Observable { get; }

    }
}
