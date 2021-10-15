using System;

namespace Ax.Fw.Bus
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EBusMsgAttribute : Attribute
    {
        public EBusMsgAttribute() { }

    }
}
