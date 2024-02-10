using System;

namespace Ax.Fw.SharedTypes.Interfaces;

/// <summary>
/// This class can subscribe to IObservable and store its last value
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IRxProperty<T> : IObservable<T>
{
  T? Value { get; }
}