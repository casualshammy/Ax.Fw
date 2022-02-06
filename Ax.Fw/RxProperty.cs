#nullable enable
using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using System;

namespace Ax.Fw
{
    public class RxProperty<T> : IRxProperty<T>
    {
        public RxProperty(IObservable<T?> _observable, ILifetime _lifetime, T? _defaultValue = default)
        {
            Value = _defaultValue;
            _observable
                .Subscribe(_x => Value = _x, _lifetime);
        }

        public T? Value { get; private set; }

    }
}
