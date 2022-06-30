#nullable enable

namespace Ax.Fw.SharedTypes.Data;

public readonly record struct TypedProgress<T>
{
    public TypedProgress(long _current, long _total, T? _currentValue)
    {
        Current = _current;
        Total = _total;
        CurrentValue = _currentValue;
    }

    public long Current { get; }
    public long Total { get; }
    public T? CurrentValue { get; }

}
