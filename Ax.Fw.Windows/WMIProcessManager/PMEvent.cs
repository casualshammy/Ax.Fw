namespace Ax.Fw.Windows.WMIProcessManager
{
    public struct PMEvent
    {
        public readonly PMEventType Event;
        public readonly int ProcessId;
        public readonly string ProcessName;

        public PMEvent(PMEventType _event, int _processId, string _processName)
        {
            Event = _event;
            ProcessId = _processId;
            ProcessName = _processName;
        }
    }
}