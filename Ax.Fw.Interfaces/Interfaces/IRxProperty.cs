#nullable enable

namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface IRxProperty<T>
    {
        T? Value { get; }
    }
}