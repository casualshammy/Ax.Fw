using Ax.Fw.Windows.WinAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ax.Fw.Windows
{
    public static class Utilities
    {
        public static IEnumerable<T> FindForms<T>() where T : Form
        {
            return Application.OpenForms.OfType<T>();
        }

        public static T GetWindow<T>() where T : Form
        {
            return FindForms<T>().FirstOrDefault();
        }

        public static async Task<T> WaitForm<T>(CancellationToken _token) where T : Form
        {
            T form;
            while ((form = GetWindow<T>()) == default && !_token.IsCancellationRequested)
                await Task.Delay(100);

            return form ?? throw new TaskCanceledException();
        }

        public static async Task PlaySystemNotificationAsync()
        {
            await Task.Run(() => NativeMethods.SndPlaySoundW("SystemNotification", Win32Consts.SND_ALIAS | Win32Consts.SND_NODEFAULT));
        }

        public static async Task PlaySystemExclamationAsync()
        {
            await Task.Run(() => NativeMethods.SndPlaySoundW("SystemExclamation", Win32Consts.SND_ALIAS | Win32Consts.SND_NODEFAULT));
        }

        public static Thread GetControlThread(Control _control)
        {
            if (_control is null)
                throw new ArgumentNullException(nameof(_control));

            Thread thread = null;
            _control.Invoke(new Action(() =>
            {
                thread = Thread.CurrentThread;
            }));
            return thread;
        }


    }
}
