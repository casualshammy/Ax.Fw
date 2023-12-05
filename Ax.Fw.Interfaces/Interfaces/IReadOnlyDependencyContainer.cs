namespace Ax.Fw.SharedTypes.Interfaces;

#if NET7_0_OR_GREATER

public interface IReadOnlyDependencyContainer
{
  T Locate<T>();
  T? LocateOrDefault<T>();
}

#endif