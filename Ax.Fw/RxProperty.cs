using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Reactive.Subjects;

namespace Ax.Fw;

/// <summary>
/// This class can subscribe to IObservable and store its last value
/// </summary>
/// <typeparam name="T"></typeparam>
public class RxProperty<T> : IRxProperty<T?>
{
  private readonly ReplaySubject<T?> p_replaySubject;

  public RxProperty(IObservable<T?> _observable, IReadOnlyLifetime _lifetime, T? _defaultValue = default)
  {
    p_replaySubject = _lifetime.DisposeOnCompleted(new ReplaySubject<T?>(1));

    Value = _defaultValue;

    _observable
      .Subscribe(_x =>
      {
        Value = _x;
        p_replaySubject.OnNext(_x);
      }, _lifetime);
  }

  public T? Value { get; private set; }

  public IDisposable Subscribe(IObserver<T?> _observer) => p_replaySubject.Subscribe(_observer);

}
