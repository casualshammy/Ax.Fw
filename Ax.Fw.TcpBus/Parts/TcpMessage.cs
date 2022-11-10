#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ax.Fw.Bus;

internal class TcpMessage
{
    private byte[]? p_serializedJson;

    [JsonConstructor]
    public TcpMessage(
        [JsonProperty(nameof(Guid))] Guid _guid,
        [JsonProperty(nameof(DataType))] string _dataType,
        [JsonProperty(nameof(Data))] JToken _data)
    {
        Guid = _guid;
        DataType = _dataType;
        Data = _data;
    }

    public Guid Guid { get; }
    public string DataType { get; }
    public JToken Data { get; }

    public async Task<byte[]> GetSerializedValue(byte[]? _password, CancellationToken _ct)
    {
        if (p_serializedJson != null)
            return p_serializedJson;

        var json = JsonConvert.SerializeObject(this);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        if (_password == null)
            return (p_serializedJson = jsonBytes);

        var encryptedBytes = await Cryptography.EncryptAes(jsonBytes, _password, _ct);
        return (p_serializedJson = encryptedBytes);
    }

}
