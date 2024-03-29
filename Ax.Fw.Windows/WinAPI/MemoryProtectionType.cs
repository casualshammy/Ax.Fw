﻿using System;

namespace Ax.Fw.Windows.WinAPI
{

    /// <summary>
    ///     Values that determine how a block of memory is protected.
    /// </summary>
    [Flags]
    public enum MemoryProtectionType
    {
        /// <summary>
        ///     Enables execute access to the committed region of pages. An attempt to read from or write to the committed region
        ///     results in an access violation.
        ///     This flag is not supported by the CreateFileMapping function.
        /// </summary>
        PAGE_EXECUTE = 0x10,

        /// <summary>
        ///     Enables execute or read-only access to the committed region of pages. An attempt to write to the committed region
        ///     results in an access violation.
        /// </summary>
        PAGE_EXECUTE_READ = 0x20,

        /// <summary>
        ///     Enables execute, read-only, or read/write access to the committed region of pages.
        /// </summary>
        PAGE_EXECUTE_READWRITE = 0x40,

        /// <summary>
        ///     Enables execute, read-only, or copy-on-write access to a mapped view of a file mapping object. An attempt to write
        ///     to a committed copy-on-write page results in a private copy of the page being made for the process. The private
        ///     page is marked as PAGE_EXECUTE_READWRITE, and the change is written to the new page.
        ///     This flag is not supported by the VirtualAlloc or VirtualAllocEx functions.
        /// </summary>
        PAGE_EXECUTE_WRITECOPY = 0x80,

        /// <summary>
        ///     Disables all access to the committed region of pages. An attempt to read from, write to, or execute the committed
        ///     region results in an access violation.
        /// </summary>
        PAGE_NOACCESS = 0x01,

        /// <summary>
        ///     Enables read-only access to the committed region of pages. An attempt to write to the committed region results in
        ///     an access violation. If Data Execution Prevention is enabled, an attempt to execute code in the committed region
        ///     results in an access violation.
        /// </summary>
        PAGE_READONLY = 0x02,

        /// <summary>
        ///     Enables read-only or read/write access to the committed region of pages. If Data Execution Prevention is enabled,
        ///     attempting to execute code in the committed region results in an access violation.
        /// </summary>
        PAGE_READWRITE = 0x04,

        /// <summary>
        ///     Enables read-only or copy-on-write access to a mapped view of a file mapping object. An attempt to write to a
        ///     committed copy-on-write page results in a private copy of the page being made for the process. The private page is
        ///     marked as PAGE_READWRITE, and the change is written to the new page. If Data Execution Prevention is enabled,
        ///     attempting to execute code in the committed region results in an access violation.
        ///     This flag is not supported by the VirtualAlloc or VirtualAllocEx functions.
        /// </summary>
        PAGE_WRITECOPY = 0x08,

        /// <summary>
        ///     Pages in the region become guard pages. Any attempt to access a guard page causes the system to raise a
        ///     STATUS_GUARD_PAGE_VIOLATION exception and turn off the guard page status. Guard pages thus act as a one-time access
        ///     alarm. For more information, see Creating Guard Pages.
        ///     When an access attempt leads the system to turn off guard page status, the underlying page protection takes over.
        ///     If a guard page exception occurs during a system service, the service typically returns a failure status indicator.
        ///     This value cannot be used with PAGE_NOACCESS.
        ///     This flag is not supported by the CreateFileMapping function.
        /// </summary>
        PAGE_GUARD = 0x100,

        /// <summary>
        ///     Sets all pages to be non-cachable. Applications should not use this attribute except when explicitly required for a
        ///     device. Using the interlocked functions with memory that is mapped with SEC_NOCACHE can result in an
        ///     EXCEPTION_ILLEGAL_INSTRUCTION exception.
        ///     The PAGE_NOCACHE flag cannot be used with the PAGE_GUARD, PAGE_NOACCESS, or PAGE_WRITECOMBINE flags.
        ///     The PAGE_NOCACHE flag can be used only when allocating private memory with the VirtualAlloc, VirtualAllocEx, or
        ///     VirtualAllocExNuma functions. To enable non-cached memory access for shared memory, specify the SEC_NOCACHE flag
        ///     when calling the CreateFileMapping function.
        /// </summary>
        PAGE_NOCACHE = 0x200,

        /// <summary>
        ///     Sets all pages to be write-combined.
        ///     Applications should not use this attribute except when explicitly required for a device. Using the interlocked
        ///     functions with memory that is mapped as write-combined can result in an EXCEPTION_ILLEGAL_INSTRUCTION exception.
        ///     The PAGE_WRITECOMBINE flag cannot be specified with the PAGE_NOACCESS, PAGE_GUARD, and PAGE_NOCACHE flags.
        ///     The PAGE_WRITECOMBINE flag can be used only when allocating private memory with the VirtualAlloc, VirtualAllocEx,
        ///     or VirtualAllocExNuma functions. To enable write-combined memory access for shared memory, specify the
        ///     SEC_WRITECOMBINE flag when calling the CreateFileMapping function.
        /// </summary>
        PAGE_WRITECOMBINE = 0x400
    }
}
