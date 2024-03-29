﻿using System;
using Ax.Fw.DependencyInjection;
using Ax.Fw.SharedTypes.Interfaces;
using System.Threading;

namespace Ax.Fw.Tests;

internal class TestAppDependency : ITestAppDependency, IAppModule<ITestAppDependency>
{
  private static long p_counter = 0L;

  public static ITestAppDependency ExportInstance(IAppDependencyCtx _ctx)
  {
    return _ctx.CreateInstance((IReadOnlyLifetime _lifetime) => new TestAppDependency(_lifetime));
  }

  public TestAppDependency(IReadOnlyLifetime _lifetime)
  {
    if (_lifetime == null)
      throw new InvalidOperationException($"Lifetime is not found!");

    Interlocked.Increment(ref p_counter);
  }

  public static long Counter => Interlocked.Read(ref p_counter);

}
