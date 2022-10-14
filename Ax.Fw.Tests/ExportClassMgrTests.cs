#nullable enable
using Ax.Fw.Attributes;
using Grace.DependencyInjection;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests;

public class ExportClassMgrTests
{
    private readonly ITestOutputHelper p_output;
    private const int p_weight = 5;

    public ExportClassMgrTests(ITestOutputHelper _output)
    {
        p_output = _output;
    }

    [Fact(Timeout = 1000)]
    public void BasicSingletonTest()
    {
        var lifetime = new Lifetime();

        var singletones = new Dictionary<Type, Func<IExportLocatorScope, object>>()
        {
            { typeof(IServiceExample), _scope => new ServiceExample(_scope.Locate<IDependancyExample>())}
        };

        var exportClassMgr = new ExportClassMgr(lifetime, singletones);

        var instance0 = exportClassMgr.Locate<IServiceExample>();
        var instance1 = exportClassMgr.Locate<IServiceExample>();
        var instance2 = exportClassMgr.Locate<IServiceExample>();

        Assert.NotNull(instance0);
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);

        Assert.Same(instance0, instance1);
        Assert.Same(instance1, instance2);

        Assert.Equal(p_weight, instance0.InstancesCount);
    }

    [Fact(Timeout = 1000)]
    public void BasicTransientTest()
    {
        var lifetime = new Lifetime();

        var exportClassMgr = new ExportClassMgr(lifetime);

        var instance0 = exportClassMgr.Locate<ITransientServiceExample>();
        var instance1 = exportClassMgr.Locate<ITransientServiceExample>();
        var instance2 = exportClassMgr.Locate<ITransientServiceExample>();

        Assert.NotNull(instance0);
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);

        Assert.NotSame(instance0, instance1);
        Assert.NotSame(instance1, instance2);
        Assert.NotSame(instance0, instance2);

        Assert.Equal(p_weight * 3, instance0.InstancesCount);
    }

    class ServiceExample : IServiceExample
    {
        private static int p_instancesCount = 0;

        public ServiceExample(IDependancyExample _dependancyExample)
        {
            p_instancesCount += _dependancyExample.Weight;
        }

        public int InstancesCount => p_instancesCount;
    }

    interface IServiceExample
    {
        int InstancesCount { get; }
    }

    [ExportClass(typeof(IDependancyExample))]
    class DependancyExample : IDependancyExample
    {
        public DependancyExample()
        {
            Weight = p_weight;
        }

        public int Weight { get; }
    }

    interface IDependancyExample
    {
        int Weight { get; }
    }

    [ExportClass(typeof(ITransientServiceExample))]
    class TransientServiceExample : ITransientServiceExample
    {
        private static int p_instancesCount = 0;

        public TransientServiceExample(IDependancyExample _dependancyExample)
        {
            p_instancesCount += _dependancyExample.Weight;
        }

        public int InstancesCount => p_instancesCount;
    }

    interface ITransientServiceExample
    {
        int InstancesCount { get; }
    }

}

