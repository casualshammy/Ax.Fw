using System;

namespace Ax.Fw.DependencyInjection;

public interface IAppDependencyCtx
{
  TOut CreateInstance<T1, TOut>(Func<T1, TOut> _func);
  TOut CreateInstance<T1, T2, TOut>(Func<T1, T2, TOut> _func);
  TOut CreateInstance<T1, T2, T3, TOut>(Func<T1, T2, T3, TOut> _func);
  TOut CreateInstance<T1, T2, T3, T4, TOut>(Func<T1, T2, T3, T4, TOut> _func);
  TOut CreateInstance<T1, T2, T3, T4, T5, TOut>(Func<T1, T2, T3, T4, T5, TOut> _func);
  TOut CreateInstance<T1, T2, T3, T4, T5, T6, TOut>(Func<T1, T2, T3, T4, T5, T6, TOut> _func);
  TOut CreateInstance<T1, T2, T3, T4, T5, T6, T7, TOut>(Func<T1, T2, T3, T4, T5, T6, T7, TOut> _func);
  TOut CreateInstance<T1, T2, T3, T4, T5, T6, T7, T8, TOut>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TOut> _func);
  TOut CreateInstance<T1, T2, T3, T4, T5, T6, T7, T8, T9, TOut>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TOut> _func);
  TOut CreateInstance<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TOut>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TOut> _func);
  T Locate<T>();
  T? LocateOrDefault<T>();
}