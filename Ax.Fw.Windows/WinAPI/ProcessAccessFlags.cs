using System;

namespace Ax.Fw.Windows.WinAPI
{
    [Flags]
    public enum ProcessAccessFlags
    {
        /// <summary>
        ///     Required to delete the object.
        /// </summary>
        DELETE = 0x00010000,

        /// <summary>
        ///     Required to read information in the security descriptor for the object, not including the information in the SACL.
        ///     To read or write the SACL, you must request the ACCESS_SYSTEM_SECURITY access right.
        ///     For more information, see SACL Access Right.
        /// </summary>
        READ_CONTROL = 0x00020000,

        /// <summary>
        ///     The right to use the object for synchronization.
        ///     This enables a thread to wait until the object is in the signaled state.
        /// </summary>
        SYNCHRONIZE = 0x00100000,

        /// <summary>
        ///     Required to modify the DACL in the security descriptor for the object.
        /// </summary>
        WRITE_DAC = 0x00040000,

        /// <summary>
        ///     Required to change the owner in the security descriptor for the object.
        /// </summary>
        WRITE_OWNER = 0x00080000,

        /// <summary>
        ///     All possible access rights for a process object.
        /// </summary>
        PROCESS_ALL_ACCESS = 0x001F0FFF,

        /// <summary>
        ///     Required to create a process.
        /// </summary>
        PROCESS_CREATE_PROCESS = 0x0080,

        /// <summary>
        ///     Required to create a thread.
        /// </summary>
        PROCESS_CREATE_THREAD = 0x0002,

        /// <summary>
        ///     Required to create a process.
        /// </summary>
        PROCESS_DUP_HANDLE = 0x0040,

        /// <summary>
        ///     Required to retrieve certain information about a process, such as its token, exit code, and priority class
        /// </summary>
        PROCESS_QUERY_INFORMATION = 0x0400,

        /// <summary>
        ///     Required to retrieve certain information about a process (see QueryFullProcessImageName).
        ///     A handle that has the PROCESS_QUERY_INFORMATION access right is automatically granted
        ///     PROCESS_QUERY_LIMITED_INFORMATION.
        /// </summary>
        PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,

        /// <summary>
        ///     Required to set certain information about a process, such as its priority class
        /// </summary>
        PROCESS_SET_INFORMATION = 0x0200,

        /// <summary>
        ///     Required to set memory limits using SetProcessWorkingSetSize.
        /// </summary>
        PROCESS_SET_QUOTA = 0x0100,

        /// <summary>
        ///     Required to suspend or resume a process.
        /// </summary>
        PROCESS_SUSPEND_RESUME = 0x0800,

        /// <summary>
        ///     Required to terminate a process using TerminateProcess.
        /// </summary>
        PROCESS_TERMINATE = 0x0001,

        /// <summary>
        ///     Required to perform an operation on the address space of a process (see VirtualProtectEx and WriteProcessMemory).
        /// </summary>
        PROCESS_VM_OPERATION = 0x0008,

        /// <summary>
        ///     Required to read memory in a process using ReadProcessMemory.
        /// </summary>
        PROCESS_VM_READ = 0x0010,

        /// <summary>
        ///     Required to write to memory in a process using WriteProcessMemory.
        /// </summary>
        PROCESS_VM_WRITE = 0x0020,
    }
}
