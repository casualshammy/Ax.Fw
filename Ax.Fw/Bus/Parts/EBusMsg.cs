using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ax.Fw.Bus.Parts
{
    internal class EBusMsg
    {
        [JsonConstructor]
        public EBusMsg(
            [JsonProperty(nameof(Type))] Type type,
            [JsonProperty(nameof(Data))] string data,
            [JsonProperty(nameof(Guid))] Guid guid)
        {
            Type = type;
            Data = data;
            Guid = guid;
        }

        public Type Type { get; }
        public string Data { get; }
        public Guid Guid { get; }
    }
}
