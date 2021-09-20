using System;

namespace Ax.Fw.SingletoneExport
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SingletoneActivatorAutoExportAttribute : Attribute
    {
        public SingletoneActivatorAutoExportAttribute(Type InterfaceType, bool ActivateOnStart = false, bool DisposeRequired = false)
        {
            this.InterfaceType = InterfaceType;
            this.ActivateOnStart = ActivateOnStart;
            this.DisposeRequired = DisposeRequired;
        }

        public Type InterfaceType { get; }
        public bool ActivateOnStart { get; }
        public bool DisposeRequired { get; }

    }

}
