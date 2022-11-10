#nullable enable

namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface ITcpBusServer
    {
        bool IsListening { get; }
        int ClientsCount { get; }
    }


}