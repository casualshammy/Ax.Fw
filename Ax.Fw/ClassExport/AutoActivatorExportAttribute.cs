#nullable enable
using System;

namespace Ax.Fw.ClassExport
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoActivatorExportAttribute : Attribute
    {
        public AutoActivatorExportAttribute(Type InterfaceType, bool Singletone = true, bool ActivateOnStart = false, bool DisposeRequired = false)
        {
            this.InterfaceType = InterfaceType;
            this.Singletone = Singletone;
            this.ActivateOnStart = ActivateOnStart;
            this.DisposeRequired = DisposeRequired;
        }

        public Type InterfaceType { get; }
        public bool Singletone { get; }
        public bool ActivateOnStart { get; }
        public bool DisposeRequired { get; }

    }

}
