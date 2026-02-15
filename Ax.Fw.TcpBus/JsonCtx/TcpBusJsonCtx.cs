using Ax.Fw.SharedTypes.Data.Bus;
using System.Text.Json.Serialization;

namespace Ax.Fw.TcpBus.JsonCtx;

[JsonSerializable(typeof(TcpMsg))]
internal partial class TcpBusJsonCtx : JsonSerializerContext
{ }
