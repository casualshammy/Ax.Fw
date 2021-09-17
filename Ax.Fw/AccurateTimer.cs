using System;
using System.Diagnostics;
using System.Threading;

namespace Ax.Fw
{
    public class AccurateTimer : IDisposable
    {
        private readonly Stopwatch p_balancingStopwatch;
        private readonly object p_lock = new();
        private readonly Action p_action;
        private readonly Action<Exception> p_onError;
        private volatile bool p_flag;
        private Thread p_thread;

        public AccurateTimer(TimeSpan _interval, Action _action, Action<Exception> _onError = null)
        {
            Interval = _interval;
            p_action = _action;
            p_onError = _onError;
            p_balancingStopwatch = Stopwatch.StartNew();
        }

        public bool IsRunning => p_flag;
        public TimeSpan Interval { get; set; }

        public AccurateTimer Start()
        {
            lock (p_lock)
            {
                if (p_thread == null)
                {
                    p_thread = new Thread(Loop) { IsBackground = true };
                    p_flag = true;
                    p_thread.Start();
                }
                return this;
            }
        }

        public void Stop()
        {
            Monitor.TryEnter(p_lock);
            try
            {

            }
            finally
            {
                Monitor.Exit(p_lock);
            }
            lock (p_lock)
            {
                if (p_thread != null)
                {
                    p_flag = false;
                    var stopped = p_thread.Join(TimeSpan.FromMinutes(1));
                    p_thread = null;
                    if (!stopped)
                        throw new Exception($"{nameof(AccurateTimer)}.{nameof(Stop)}: Can't stop!");
                }
            }
            lock (p_lock)
            {
                if (p_thread != null)
                {
                    p_flag = false;
                    var stopped = p_thread.Join(TimeSpan.FromMinutes(1));
                    p_thread = null;
                    if (!stopped)
                        throw new Exception($"{nameof(AccurateTimer)}.{nameof(Stop)}: Can't stop!");
                }
            }
        }

        private void Loop()
        {
            TimeSpan maxThreadSleepTime = TimeSpan.FromMilliseconds(100);
            while (p_flag)
            {
                p_balancingStopwatch.Restart();
                try
                {
                    p_action();
                }
                catch (Exception ex)
                {
                    p_onError?.Invoke(ex);
                }
                TimeSpan timeWait;
                while ((timeWait = Interval - p_balancingStopwatch.Elapsed) > TimeSpan.Zero && p_flag)
                    Thread.Sleep(timeWait > maxThreadSleepTime ? maxThreadSleepTime : timeWait);
            }
        }

        public void Dispose()
        {
            Stop();
        }

    }
}
