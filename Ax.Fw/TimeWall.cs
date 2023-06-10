using System;

namespace Ax.Fw;

public class TimeWall
{
  private readonly int?[] p_slots;
  private readonly int p_timeFrameMs;
  private readonly object p_lock = new();

  public TimeWall(int _ticketsAllowedInTimeFrame, TimeSpan _timeFrame)
  {
    p_timeFrameMs = (int)_timeFrame.TotalMilliseconds;
    p_slots = new int?[_ticketsAllowedInTimeFrame];
  }

  public bool TryGetTicket()
  {
    lock (p_lock)
    {
      var now = Environment.TickCount;
      for (int i = 0; i < p_slots.Length; i++)
      {
        var entry = p_slots[i];
        if (entry == null || now - entry > p_timeFrameMs)
        {
          p_slots[i] = now;
          return true;
        }
      }

      return false;
    }
  }


}
