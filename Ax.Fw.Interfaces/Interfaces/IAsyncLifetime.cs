using System.Threading.Tasks;

namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface IAsyncLifetime : IAsyncReadOnlyLifetime
    {
        Task Complete();
    }

}