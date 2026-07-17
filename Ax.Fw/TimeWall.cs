using System;

namespace Ax.Fw;

/// <summary>
/// A time wall that allows a certain number of tickets to be issued in a given time frame.
/// </summary>
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

  /// <summary>
  /// Tries to get a ticket. If the number of tickets allowed in the time frame has been reached, returns false.
  /// </summary>
  /// <remarks>Thread-safe.</remarks>
  /// <returns></returns>
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
