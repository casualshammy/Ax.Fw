using System;

namespace Ax.Fw.SingletoneExport
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SingletoneActivatorAutoExportAttribute : Attribute
    {
        public SingletoneActivatorAutoExportAttribute(Type interfaceType, bool singleton, bool activateOnStart)
        {
            InterfaceType = interfaceType;
            Singleton = singleton;
            ActivateOnStart = activateOnStart;
        }

        public Type InterfaceType { get; }
        public bool Singleton { get; }
        public bool ActivateOnStart { get; }
    }

}
