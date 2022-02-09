#nullable enable
using Ax.Fw.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests
{
    public class RxPropertyTests
    {
        private readonly ITestOutputHelper p_output;

        public RxPropertyTests(ITestOutputHelper output)
        {
            p_output = output;
        }

        [Fact]
        public void TestRxProperty()
        {
            var lifetime = new Lifetime();
            try
            {
                var rxProp = Observable
                    .Return(100)
                    .Concat(Observable.Return(200))
                    .Concat(Observable.Return(300))
                    .ToProperty(lifetime);

                Assert.Equal(300, rxProp.Value);
            }
            finally
            {
                lifetime.Complete();
            }
        }

        [Fact]
        public void TestEmptyRxProperty()
        {
            var lifetime = new Lifetime();
            try
            {
                var rxProp = Observable
                    .Interval(TimeSpan.FromMilliseconds(250))
                    .Select(_x => (long?)_x)
                    .ToProperty(lifetime);

                Assert.Null(rxProp.Value);
            }
            finally
            {
                lifetime.Complete();
            }
        }

    }
}
