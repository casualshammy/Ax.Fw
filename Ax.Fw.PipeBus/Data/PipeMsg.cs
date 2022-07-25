using Ax.Fw.SharedTypes.Interfaces;

namespace Ax.Fw.PipeBus.Data;

[Serializable]
internal class PipeMsg
{
    public PipeMsg() { }

    public PipeMsg(Guid _guid, string? _type, IBusMsg? _jsonData)
    {
        Guid = _guid;
        Type = _type;
        Data = _jsonData;
    }

    public Guid Guid { get; init; }
    public string? Type { get; init; }
    public IBusMsg? Data { get; init; }

    public override int GetHashCode() => HashCode.Combine(Guid, Type);

    public override bool Equals(object? _obj)
    {
        if (_obj is not PipeMsg pipeMsg)
            return false;

        return pipeMsg.Guid == Guid;
    }

}
