using Ax.Fw.Windows.WinAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace Ax.Fw.Windows
{
    public static class Extensions
    {

        /// <summary>
        ///     Very fast comparison of byte arrays (memcmp)
        /// </summary>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns>True if sequences are equal, false otherwise</returns>
        public static unsafe bool MemCmp(this byte[] b1, byte[] b2)
        {
            fixed (byte* v1 = b1)
            fixed (byte* v2 = b2)
                return WinAPI.NativeMethods.memcmp(v1, v2, (UIntPtr)b1.Length) == 0;
        }

        public static void ActivateBrutal(this Form form)
        {
            form.Show();
            form.WindowState = FormWindowState.Normal;
            form.Activate();
        }

        public static IEnumerable<ToolStripItem> GetAllToolStripItems(this ToolStripItemCollection collection)
        {
            foreach (ToolStripItem toolStripItem in collection)
            {
                if (toolStripItem != null)
                {
                    yield return toolStripItem;
                    if (toolStripItem is ToolStripDropDownItem item && item.HasDropDownItems)
                        foreach (ToolStripItem v in GetAllToolStripItems(item.DropDownItems))
                            yield return v;
                }
            }
        }

        public static void SetProcessPrioritiesToNormal(this Process _process)
        {
            if (_process != null)
            {
                var normalMemoryPriority = new IntPtr(5);
                var processMemoryPriority = 0x27;
                NativeMethods.NtSetInformationProcess(_process.Handle, processMemoryPriority, ref normalMemoryPriority, 0x4);
                var normalIoPriority = new IntPtr(2);
                var processIoPriority = 0x21;
                NativeMethods.NtSetInformationProcess(_process.Handle, processIoPriority, ref normalIoPriority, 0x4);
                _process.PriorityClass = ProcessPriorityClass.Normal;
            }
        }

    }
}
