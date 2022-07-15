#nullable enable
using System;

namespace Ax.Fw.SharedTypes.Data.Workers
{
    public class PenaltyInfo
    {
        public PenaltyInfo(bool _tryAgain, TimeSpan? _delay)
        {
            TryAgain = _tryAgain;
            Delay = _delay;
        }

        public bool TryAgain { get; }
        public TimeSpan? Delay { get; }

    }
}
