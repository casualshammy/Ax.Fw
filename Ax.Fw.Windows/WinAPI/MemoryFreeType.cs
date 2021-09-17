using System;

namespace Ax.Fw.Windows.WinAPI
{
    /// <summary>
    ///     Values that determine how a block of memory is freed.
    /// </summary>
    [Flags]
    public enum MemoryFreeType
    {
        /// <summary>
        ///     Decommits the specified region of committed pages. After the operation, the pages are in the reserved state.
        ///     The function does not fail if you attempt to decommit an uncommitted page. This means that you can decommit a range
        ///     of pages without first determining the current commitment state.
        ///     Do not use this value with MEM_RELEASE.
        /// </summary>
        MEM_DECOMMIT = 0x4000,

        /// <summary>
        ///     Releases the specified region of pages. After this operation, the pages are in the free state.
        ///     If you specify this value, dwSize must be 0 (zero), and lpAddress must point to the base address returned by the
        ///     VirtualAlloc function when the region is reserved. The function fails if either of these conditions is not met.
        ///     If any pages in the region are committed currently, the function first decommits, and then releases them.
        ///     The function does not fail if you attempt to release pages that are in different states, some reserved and some
        ///     committed. This means that you can release a range of pages without first determining the current commitment state.
        ///     Do not use this value with MEM_DECOMMIT.
        /// </summary>
        MEM_RELEASE = 0x8000
    }
}
