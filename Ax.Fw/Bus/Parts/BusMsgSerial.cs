#nullable enable
using Ax.Fw.SharedTypes.Interfaces;
using System;

namespace Ax.Fw.Bus.Parts
{
    internal class BusMsgSerial
    {
        public BusMsgSerial(IBusMsg _data, Guid _id)
        {
            Data = _data;
            Id = _id;
        }

        public IBusMsg Data { get; }
        public Guid Id { get; }
    }
}
