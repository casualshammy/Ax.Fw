using System;

namespace Ax.Fw.SharedTypes.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class SimpleDocumentAttribute : Attribute
{
    public SimpleDocumentAttribute(string _namespace)
    {
        Namespace = _namespace;
    }

    public string Namespace { get; }

}
