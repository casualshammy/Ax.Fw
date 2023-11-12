using System;

namespace Ax.Fw.SharedTypes.Data.Bus;

public record TcpMsg(Guid Guid, string TypeSlug, byte[] Data);

//internal class TcpMessage
//{
//    private volatile byte[]? p_serializedJson;

//    [JsonConstructor]
//    public TcpMessage(
//        [JsonProperty(nameof(Guid))] Guid _guid,
//        [JsonProperty(nameof(TypeSlug))] string _dataType,
//        [JsonProperty(nameof(Data))] JToken _data)
//    {
//        Guid = _guid;
//        TypeSlug = _dataType;
//        Data = _data;
//    }

//    public Guid Guid { get; }
//    public string TypeSlug { get; }
//    public JToken Data { get; }

//    public async Task<byte[]> GetSerializedValue(byte[]? _password, CancellationToken _ct)
//    {
//        if (p_serializedJson != null)
//            return p_serializedJson;

//        var json = JsonConvert.SerializeObject(this);
//        var jsonBytes = Encoding.UTF8.GetBytes(json);
//        if (_password == null)
//            return (p_serializedJson = jsonBytes);

//        var encryptedBytes = await Cryptography.EncryptAes(jsonBytes, _password, true, _ct);
//        return (p_serializedJson = encryptedBytes);
//    }

//}
