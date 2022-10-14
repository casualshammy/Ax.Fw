using System;

namespace Ax.Fw.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    [Obsolete($"Please use {nameof(ExportClassAttribute)}")]
    public class ImportClassAttribute : Attribute
    {
        public ImportClassAttribute(Type InterfaceType, bool Singleton = false, bool ActivateOnStart = false, bool DisposeRequired = false)
        {
            this.InterfaceType = InterfaceType;
            this.Singleton = Singleton;
            this.ActivateOnStart = ActivateOnStart;
            this.DisposeRequired = DisposeRequired;
        }

        public Type InterfaceType { get; }
        public bool Singleton { get; }
        public bool ActivateOnStart { get; }
        public bool DisposeRequired { get; }

    }

}
