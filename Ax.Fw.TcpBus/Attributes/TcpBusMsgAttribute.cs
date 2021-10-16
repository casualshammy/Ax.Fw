#nullable enable
using System;

namespace Ax.Fw.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TcpBusMsgAttribute : Attribute
    {
        public TcpBusMsgAttribute() { }

    }
}
