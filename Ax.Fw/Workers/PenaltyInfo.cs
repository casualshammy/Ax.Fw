#nullable enable
using System;

namespace Ax.Fw.Workers
{
    public class PenaltyInfo
    {
        public PenaltyInfo(bool tryAgain, TimeSpan? delay)
        {
            TryAgain = tryAgain;
            Delay = delay;
        }

        public bool TryAgain { get; }
        public TimeSpan? Delay { get; }

    }
}
