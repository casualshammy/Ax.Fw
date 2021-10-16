#nullable enable
using Ax.Fw.Interfaces;
using System;

namespace Ax.Fw.TcpBus.Parts
{
    internal class BusMsgSerial
    {
        public BusMsgSerial(IBusMsg data, Guid id)
        {
            Data = data;
            Id = id;
        }

        public IBusMsg Data { get; }
        public Guid Id { get; }
    }
}
