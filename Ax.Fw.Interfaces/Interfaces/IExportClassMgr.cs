namespace Ax.Fw.SharedTypes.Interfaces;

public interface IExportClassMgr
{
    T Locate<T>() where T : notnull;
    T? LocateOrNull<T>() where T : notnull;
}