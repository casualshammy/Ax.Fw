using System;
using System.Management;

namespace Ax.Fw.Windows.WMIProcessManager
{
    public class ProcessManager : IDisposable
    {
        private ManagementEventWatcher p_watcherStart;
        private ManagementEventWatcher p_watcherStop;
        private bool p_disposedValue;
        private readonly object p_wmiLock = new();

        public event Action<PMEvent> Events;

        public ProcessManager()
        {
            lock (p_wmiLock)
            {
                p_watcherStart = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
                p_watcherStart.EventArrived += ProcessStarted;
                p_watcherStart.Start();

                p_watcherStop = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
                p_watcherStop.EventArrived += ProcessStopped;
                p_watcherStop.Start();
            }
        }

        private void ProcessStopped(object sender, EventArrivedEventArgs e)
        {
            Events?.Invoke(new PMEvent(PMEventType.ProcessStopped, int.Parse(e.NewEvent["ProcessID"].ToString()), e.NewEvent["ProcessName"].ToString()));
        }

        private void ProcessStarted(object sender, EventArrivedEventArgs e)
        {
            Events?.Invoke(new PMEvent(PMEventType.ProcessStarted, int.Parse(e.NewEvent["ProcessID"].ToString()), e.NewEvent["ProcessName"].ToString()));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!p_disposedValue)
            {
                if (disposing)
                {
                    lock (p_wmiLock)
                    {
                        if (p_watcherStart != null)
                        {
                            p_watcherStart.EventArrived -= ProcessStarted;
                            p_watcherStart.Stop();
                            p_watcherStart.Dispose();
                            p_watcherStart = null;
                        }
                        if (p_watcherStop != null)
                        {
                            p_watcherStop.EventArrived -= ProcessStopped;
                            p_watcherStop.Stop();
                            p_watcherStop.Dispose();
                            p_watcherStop = null;
                        }
                    }
                }

                p_disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
}
