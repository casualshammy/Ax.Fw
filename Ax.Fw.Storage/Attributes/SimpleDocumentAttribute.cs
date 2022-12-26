namespace Ax.Fw.Storage.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class SimpleDocumentAttribute : Attribute
{
    public SimpleDocumentAttribute(string _namespace)
    {
        Namespace = _namespace;
    }

    public string Namespace { get; }

}
