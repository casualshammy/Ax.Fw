using System;

namespace Ax.Fw.SharedTypes.Data.Workers;

public readonly struct PenaltyInfo
{
  public PenaltyInfo(bool _tryAgain, TimeSpan? _delay)
  {
    TryAgain = _tryAgain;
    Delay = _delay;
  }

  public readonly bool TryAgain;
  public readonly TimeSpan? Delay;

}
