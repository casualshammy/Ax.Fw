using System.IO;

namespace Ax.Fw.JsonStorages.Parts;

internal class FileInfoChanges
{
    public FileInfoChanges(FileInfo? _fileInfo, bool _changed)
    {
        FileInfo = _fileInfo;
        Changed = _changed;
    }

    public FileInfo? FileInfo { get; }
    public bool Changed { get; }
}
