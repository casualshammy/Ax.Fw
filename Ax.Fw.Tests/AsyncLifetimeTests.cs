#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests
{
    public class AsyncLifetimeTests
    {
        private readonly ITestOutputHelper p_output;

        public AsyncLifetimeTests(ITestOutputHelper output)
        {
            p_output = output;
        }

        [Fact(Timeout = 30000)]
        public async Task FlowTest()
        {
            var lifetime = new AsyncLifetime();

            var counter = 0;
            lifetime.DoOnCompleted(() => Interlocked.Increment(ref counter));
            lifetime.DoOnCompleted(async () =>
            {
                Interlocked.Increment(ref counter);
                await Task.Delay(100);
            });

            await lifetime.CompleteAsync();

            Assert.Equal(2, counter);

        }


    }
}
