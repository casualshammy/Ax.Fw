using System.Runtime.InteropServices;

namespace Ax.Fw.Cache.Parts;

[StructLayout(LayoutKind.Sequential,
  CharSet = CharSet.Unicode,
  Size = Size,
  Pack = 1)]
internal struct FileCacheEntryHeader
{
    public const int Size = 512;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public required string Mime;
}
