namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface ILifetime : IReadOnlyLifetime
    {
        void Complete();
    }
}