#nullable enable
using Ax.Fw.SharedTypes.Interfaces;
using System;

namespace Ax.Fw.SharedTypes.Data.Bus;

public class BusMsgSerial
{
    public BusMsgSerial(IBusMsg data, Guid id)
    {
        Data = data;
        Id = id;
    }

    public IBusMsg Data { get; }
    public Guid Id { get; }
}
