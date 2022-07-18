namespace Ax.Fw.PipeBus.Data;

[Serializable]
internal class PipeMsg
{
    public PipeMsg() { }

    public PipeMsg(Guid _guid, string? _type, string? _jsonData)
    {
        Guid = _guid;
        Type = _type;
        JsonData = _jsonData;
    }

    public Guid Guid { get; init; }
    public string? Type { get; init; }
    public string? JsonData { get; init; }

    public override int GetHashCode() => HashCode.Combine(Guid, Type);

    public override bool Equals(object? _obj)
    {
        if (_obj is not PipeMsg pipeMsg)
            return false;

        return pipeMsg.Guid == Guid;
    }

}
