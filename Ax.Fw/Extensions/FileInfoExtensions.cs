using System.IO;

namespace Ax.Fw.Extensions;

public static class FileInfoExtensions
{
    public static bool TryDelete(this FileInfo _fileInfo)
    {
        try
        {
            _fileInfo.Delete();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
