using System;

namespace Ax.Fw.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ExportClassAttribute : Attribute
{
    public ExportClassAttribute(Type InterfaceType, bool Singleton = false, bool ActivateOnStart = false)
    {
        this.InterfaceType = InterfaceType;
        this.Singleton = Singleton;
        this.ActivateOnStart = ActivateOnStart;
    }

    public Type InterfaceType { get; }
    public bool Singleton { get; }
    public bool ActivateOnStart { get; }

}

