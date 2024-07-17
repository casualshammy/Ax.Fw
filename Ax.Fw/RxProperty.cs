using Ax.Fw.Extensions;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Reactive.Subjects;

namespace Ax.Fw;

/// <summary>
/// This class can subscribe to IObservable and store its last value
/// </summary>
/// <typeparam name="T"></typeparam>
public class RxProperty<T> : IRxProperty<T>
{
  private readonly BehaviorSubject<T> p_flow;

  public RxProperty(IObservable<T> _observable, IReadOnlyLifetime _lifetime, T _defaultValue = default)
  {
    p_flow = _lifetime.ToDisposeOnEnded(new BehaviorSubject<T>(_defaultValue));

    _observable
      .Subscribe(_x => p_flow.OnNext(_x), _lifetime);
  }

  public T Value => p_flow.Value;

  public IDisposable Subscribe(IObserver<T> _observer) => p_flow.Subscribe(_observer);

}
