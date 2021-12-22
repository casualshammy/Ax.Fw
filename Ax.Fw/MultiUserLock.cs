using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Ax.Fw
{
    public class MultiUserLock
    {
        private readonly object p_internalLock = new();
        private readonly ConcurrentDictionary<Guid, object> p_lockObjects = new();

        /// <summary>
        /// Get lock. This instance of <see cref="MultiLock"/> will be in signaled state.
        /// You can get multiple locks on one instance of <see cref="MultiLock"/>.
        /// To release lock, use <see cref="ReleaseLock"/> method.
        /// </summary>
        /// <returns></returns>
        public Guid GetLock()
        {
            lock (p_internalLock)
            {
                Guid guid = Guid.NewGuid();
                p_lockObjects.TryAdd(guid, new object());
                return guid;
            }
        }

        /// <summary>
        /// Release lock. If <paramref name="guid"/> is not found, <see cref="KeyNotFoundException"/> will be raised
        /// </summary>
        /// <param name="guid"></param>
        public void ReleaseLock(Guid guid)
        {
            lock (p_internalLock)
                if (p_lockObjects.ContainsKey(guid))
                    p_lockObjects.TryRemove(guid, out _);
                else
                    throw new KeyNotFoundException("This GUID is not registered");
        }

        public bool HasLocks
        {
            get
            {
                lock (p_internalLock)
                {
                    return p_lockObjects.Count > 0;
                }
            }
        }

        /// <summary>
        /// Wait for all locks to be released
        /// </summary>
        /// <param name="timeoutMs"></param>
        public async Task WaitForLocksAsync(long timeoutMs = long.MaxValue)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            while (p_lockObjects.Count > 0 && stopwatch.ElapsedMilliseconds < timeoutMs)
                await Task.Delay(1);
        }

    }
}
