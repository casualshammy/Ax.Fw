using Ax.Fw.SharedTypes.Interfaces;

namespace Ax.Fw.App.Data;

public class ObservableConfig<T> : IObservableConfig<T?>
{
  private readonly IObservable<T?> p_observable;

  public ObservableConfig(IObservable<T?> _observable)
  {
    p_observable = _observable;
  }

  public IDisposable Subscribe(IObserver<T?> _observer) => p_observable.Subscribe(_observer);

}