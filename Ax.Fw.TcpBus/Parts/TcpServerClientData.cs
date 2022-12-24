using System;
using System.Collections.Generic;

namespace Ax.Fw.TcpBus.Parts;

internal class TcpServerClientData
{
    public TcpServerClientData(Guid _clientGuid, byte[] _data, Dictionary<string, object> _metadata)
    {
        ClientGuid = _clientGuid;
        Data = _data;
        Metadata = _metadata;
    }

    public Guid ClientGuid { get; }
    public byte[] Data { get; }
    public Dictionary<string, object> Metadata { get; }

}
