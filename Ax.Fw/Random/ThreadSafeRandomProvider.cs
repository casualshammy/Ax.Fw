using System;
using System.Threading;

namespace Ax.Fw.Rnd
{
    public class ThreadSafeRandomProvider
    {
        private static int p_seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> p_randomWrapper = new(() => new Random(Interlocked.Increment(ref p_seed)));

        public static Random GetThreadRandom() => p_randomWrapper.Value;

    }
}
