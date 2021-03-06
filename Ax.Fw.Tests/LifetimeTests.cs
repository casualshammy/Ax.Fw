#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ax.Fw.Tests
{
    public class LifetimeTests
    {
        private readonly ITestOutputHelper p_output;

        public LifetimeTests(ITestOutputHelper output)
        {
            p_output = output;
        }

        [Fact(Timeout = 30000)]
        public async Task FlowTest()
        {
            var lifetime = new Lifetime();

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

        [Fact(Timeout = 30000)]
        public void SyncCompleteTest()
        {
            var lifetime = new Lifetime();

            var counter = 0;
            lifetime.DoOnCompleted(() => Interlocked.Increment(ref counter));
            lifetime.DoOnCompleted(async () =>
            {
                Interlocked.Increment(ref counter);
                await Task.Delay(500);
            });

            lifetime.Complete();

            Assert.Equal(2, counter);

        }

        [Fact(Timeout = 30000)]
        public void MultipleCompleteTest()
        {
            var lifetime = new Lifetime();

            var counter = 0;
            lifetime.DoOnCompleted(() => Interlocked.Increment(ref counter));
            lifetime.DoOnCompleted(async () =>
            {
                Interlocked.Increment(ref counter);
                await Task.Delay(500);
            });

            lifetime.Complete();
            lifetime.Complete();

            Assert.Equal(2, counter);

            Thread.Sleep(500);
            lifetime.Complete();
            lifetime.Complete();
        }

        [Fact(Timeout = 30000)]
        public void ParallelCompleteTest()
        {
            var lifetime = new Lifetime();

            var counter = 0;
            lifetime.DoOnCompleted(() => Interlocked.Increment(ref counter));
            lifetime.DoOnCompleted(async () =>
            {
                Interlocked.Increment(ref counter);
                await Task.Delay(500);
            });

            Parallel.For(0, 100, _ =>
            {
                lifetime.Complete();
            });
            Thread.Sleep(500);
            Parallel.For(0, 100, _ =>
            {
                lifetime.Complete();
            });

            Assert.Equal(2, counter);
        }

        [Fact(Timeout = 30000)]
        public void ParallelCompleteAsyncTest()
        {
            var lifetime = new Lifetime();

            var counter = 0;
            lifetime.DoOnCompleted(() => Interlocked.Increment(ref counter));
            lifetime.DoOnCompleted(async () =>
            {
                Interlocked.Increment(ref counter);
                await Task.Delay(500);
            });

            Parallel.For(0, 100, _ => lifetime.CompleteAsync());

            Thread.Sleep(250);
            Assert.Equal(1, counter);

            Thread.Sleep(750);
            Assert.Equal(2, counter);

            Parallel.For(0, 100, _ => lifetime.CompleteAsync());
        }


    }
}
