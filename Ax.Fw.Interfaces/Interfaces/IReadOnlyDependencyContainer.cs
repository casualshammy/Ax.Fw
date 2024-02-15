namespace Ax.Fw.SharedTypes.Interfaces;

public interface IReadOnlyDependencyContainer
{
  T Locate<T>();
  T? LocateOrDefault<T>();
}
