namespace Ax.Fw.Windows.GUI.Forms
{
    internal class DelayedCall<T> : DelayedCall
    {
        public new delegate void Callback(T data);

        private Callback callback;

        private T data;

        public static DelayedCall<T> Create(Callback cb, T data, int milliseconds)
        {
            DelayedCall<T> delayedCall = new DelayedCall<T>();
            DelayedCall.PrepareDCObject(delayedCall, milliseconds, async: false);
            delayedCall.callback = cb;
            delayedCall.data = data;
            return delayedCall;
        }

        public static DelayedCall<T> CreateAsync(Callback cb, T data, int milliseconds)
        {
            DelayedCall<T> delayedCall = new DelayedCall<T>();
            DelayedCall.PrepareDCObject(delayedCall, milliseconds, async: true);
            delayedCall.callback = cb;
            delayedCall.data = data;
            return delayedCall;
        }

        public static DelayedCall<T> Start(Callback cb, T data, int milliseconds)
        {
            DelayedCall<T> delayedCall = Create(cb, data, milliseconds);
            delayedCall.Start();
            return delayedCall;
        }

        public static DelayedCall<T> StartAsync(Callback cb, T data, int milliseconds)
        {
            DelayedCall<T> delayedCall = CreateAsync(cb, data, milliseconds);
            delayedCall.Start();
            return delayedCall;
        }

        protected override void OnFire()
        {
            context.Post(delegate
            {
                lock (timerLock)
                {
                    if (cancelled)
                    {
                        return;
                    }
                }

                if (callback != null)
                {
                    callback(data);
                }
            }, null);
        }

        public void Reset(T data, int milliseconds)
        {
            lock (timerLock)
            {
                Cancel();
                this.data = data;
                base.Milliseconds = milliseconds;
                Start();
            }
        }
    }

}
