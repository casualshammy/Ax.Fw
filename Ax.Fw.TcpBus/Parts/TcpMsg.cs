#nullable enable
using System.Collections.Generic;

namespace Ax.Fw.TcpBus.Parts
{
    internal class TcpMsg
    {
        public TcpMsg(
            string _jsonData, 
            Dictionary<object, object> _meta)
        {
            JsonData = _jsonData;
            Meta = _meta;
        }

        public string JsonData { get; }
        public Dictionary<object, object> Meta { get; }
    }
}
