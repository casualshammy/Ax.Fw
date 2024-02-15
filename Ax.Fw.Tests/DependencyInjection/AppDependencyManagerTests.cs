using Ax.Fw.DependencyInjection;
using Ax.Fw.SharedTypes.Interfaces;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests;

public class AppDependencyManagerTests
{
  private readonly ITestOutputHelper p_output;

  public AppDependencyManagerTests(ITestOutputHelper _output)
  {
    p_output = _output;
  }

  [Fact(Timeout = 30000)]
  public void SimpleTestAsync()
  {
    var appDepMgr = AppDependencyManager
      .Create()
      .AddSingleton<IReadOnlyLifetime>(new Lifetime())
      .AddModule<TestAppDependency, ITestAppDependency>()
      .ActivateOnStart<ITestAppDependency>()
      .Build();

    var lifetime0 = appDepMgr.LocateOrDefault<IReadOnlyLifetime>();
    var lifetime1 = appDepMgr.LocateOrDefault<IReadOnlyLifetime>();
    Assert.NotNull(lifetime0);
    Assert.Equal(lifetime0, lifetime1);

    Assert.Equal(1L, TestAppDependency.Counter);

    var testAppDependency0 = appDepMgr.LocateOrDefault<ITestAppDependency>();
    var testAppDependency1 = appDepMgr.LocateOrDefault<ITestAppDependency>();
    Assert.NotNull(testAppDependency0);
    Assert.Equal(testAppDependency0, testAppDependency1);
  }

  [Fact(Timeout = 30000)]
  public void EqualTypeAddedExceptionAsync()
  {
    Assert.Throws<InvalidOperationException>(() =>
      AppDependencyManager
        .Create()
        .AddSingleton<IReadOnlyLifetime>(new Lifetime())
        .AddSingleton<IReadOnlyLifetime>(new Lifetime()));
  }

  [Fact(Timeout = 30000)]
  public void DoubleBuildExceptionAsync()
  {
    Assert.Throws<InvalidOperationException>(() =>
      AppDependencyManager
        .Create()
        .AddSingleton<IReadOnlyLifetime>(new Lifetime())
        .Build()
        .Build());
  }

  [Fact(Timeout = 30000)]
  public void UnknownTypeExceptionAsync()
  {
    var appDepMgr = AppDependencyManager
        .Create()
        .AddSingleton<IReadOnlyLifetime>(new Lifetime())
        .Build();

    Assert.NotNull(appDepMgr.LocateOrDefault<IReadOnlyLifetime>());
    Assert.Null(appDepMgr.LocateOrDefault<ILifetime>());
    Assert.Throws<KeyNotFoundException>(() => appDepMgr.Locate<ILifetime>());
  }

}
