using System;

namespace Ax.Fw.DependencyInjection;

#if NET7_0_OR_GREATER

public class AppDependencyCtx : IAppDependencyCtx
{
  private readonly AppDependencyManager p_mgr;

  internal AppDependencyCtx(AppDependencyManager _appDependencyManager)
  {
    p_mgr = _appDependencyManager;
  }

  public T Locate<T>() => p_mgr.Locate<T>();

  public T? LocateOrDefault<T>() => p_mgr.LocateOrDefault<T>();

  public TOut CreateInstance<TOut>(Func<TOut> _func) => _func();

  public TOut CreateInstance<T1, TOut>(Func<T1, TOut> _func)
    => _func(p_mgr.Locate<T1>());

  public TOut CreateInstance<T1, T2, TOut>(Func<T1, T2, TOut> _func)
    => _func(p_mgr.Locate<T1>(), p_mgr.Locate<T2>());

  public TOut CreateInstance<T1, T2, T3, TOut>(Func<T1, T2, T3, TOut> _func)
    => _func(p_mgr.Locate<T1>(), p_mgr.Locate<T2>(), p_mgr.Locate<T3>());

  public TOut CreateInstance<T1, T2, T3, T4, TOut>(Func<T1, T2, T3, T4, TOut> _func)
    => _func(p_mgr.Locate<T1>(), p_mgr.Locate<T2>(), p_mgr.Locate<T3>(), p_mgr.Locate<T4>());

  public TOut CreateInstance<T1, T2, T3, T4, T5, TOut>(Func<T1, T2, T3, T4, T5, TOut> _func)
    => _func(p_mgr.Locate<T1>(), p_mgr.Locate<T2>(), p_mgr.Locate<T3>(), p_mgr.Locate<T4>(), p_mgr.Locate<T5>());

  public TOut CreateInstance<T1, T2, T3, T4, T5, T6, TOut>(Func<T1, T2, T3, T4, T5, T6, TOut> _func)
    => _func(p_mgr.Locate<T1>(), p_mgr.Locate<T2>(), p_mgr.Locate<T3>(), p_mgr.Locate<T4>(), p_mgr.Locate<T5>(), p_mgr.Locate<T6>());

  public TOut CreateInstance<T1, T2, T3, T4, T5, T6, T7, TOut>(Func<T1, T2, T3, T4, T5, T6, T7, TOut> _func)
    => _func(p_mgr.Locate<T1>(), p_mgr.Locate<T2>(), p_mgr.Locate<T3>(), p_mgr.Locate<T4>(), p_mgr.Locate<T5>(), p_mgr.Locate<T6>(), p_mgr.Locate<T7>());

  public TOut CreateInstance<T1, T2, T3, T4, T5, T6, T7, T8, TOut>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TOut> _func)
    => _func(p_mgr.Locate<T1>(), p_mgr.Locate<T2>(), p_mgr.Locate<T3>(), p_mgr.Locate<T4>(), p_mgr.Locate<T5>(), p_mgr.Locate<T6>(), p_mgr.Locate<T7>(), p_mgr.Locate<T8>());

  public TOut CreateInstance<T1, T2, T3, T4, T5, T6, T7, T8, T9, TOut>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TOut> _func)
    => _func(p_mgr.Locate<T1>(), p_mgr.Locate<T2>(), p_mgr.Locate<T3>(), p_mgr.Locate<T4>(), p_mgr.Locate<T5>(), p_mgr.Locate<T6>(), p_mgr.Locate<T7>(), p_mgr.Locate<T8>(), p_mgr.Locate<T9>());

  public TOut CreateInstance<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TOut>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TOut> _func)
    => _func(p_mgr.Locate<T1>(), p_mgr.Locate<T2>(), p_mgr.Locate<T3>(), p_mgr.Locate<T4>(), p_mgr.Locate<T5>(), p_mgr.Locate<T6>(), p_mgr.Locate<T7>(), p_mgr.Locate<T8>(), p_mgr.Locate<T9>(), p_mgr.Locate<T10>());

}
#endif