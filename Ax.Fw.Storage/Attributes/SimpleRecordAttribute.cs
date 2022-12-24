namespace Ax.Fw.Storage.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class SimpleRecordAttribute : Attribute
{
    public SimpleRecordAttribute(string _tableName)
    {
        TableName = _tableName;
    }

    public string TableName { get; }

}
