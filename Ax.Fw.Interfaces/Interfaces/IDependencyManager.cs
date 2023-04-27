namespace Ax.Fw.SharedTypes.Interfaces;

public interface IDependencyManager
{
  T Locate<T>() where T : notnull;
  T? LocateOrDefault<T>() where T : notnull;
}