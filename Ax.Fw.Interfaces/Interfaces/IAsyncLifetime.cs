#nullable enable
using System.Threading.Tasks;

namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface IAsyncLifetime : IAsyncReadOnlyLifetime
    {
        void Complete();
        Task CompleteAsync();
    }

}