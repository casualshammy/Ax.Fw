using System;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface IRxProperty<out T> : IObservable<T>
{
  T Value { get; }
}