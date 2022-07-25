using System;

namespace Ax.Fw;

public class TimeWall
{
    private readonly int?[] p_slots;
    private readonly int p_ticketsAllowedInTimeFrame;
    private readonly int p_timeFrameMs;
    private readonly object p_lock = new();

    public TimeWall(int _ticketsAllowedInTimeFrame, TimeSpan _timeFrame)
    {
        p_ticketsAllowedInTimeFrame = _ticketsAllowedInTimeFrame;
        p_timeFrameMs = (int)_timeFrame.TotalMilliseconds;
        p_slots = new int?[p_ticketsAllowedInTimeFrame];
    }

    public bool TryGetTicket()
    {
        lock (p_lock)
        {
            var now = Environment.TickCount;
            for (int i = 0; i < p_ticketsAllowedInTimeFrame; i++)
            {
                var entry = p_slots[i];
                if (entry != null && now - entry > p_timeFrameMs)
                    p_slots[i] = null;
            }

            var firstFreeIndex = Array.IndexOf(p_slots, null);
            if (firstFreeIndex == -1)
                return false;

            p_slots[firstFreeIndex] = now;
            return true;
        }
    }


}
