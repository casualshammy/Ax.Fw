#nullable enable

namespace Ax.Fw.SharedTypes.Data
{
    public class TeamState
    {
        public TeamState(
            int _tasksRunning,
            int _tasksWaitingForExecution,
            int _tasksCompleted,
            int _tasksFailed)
        {
            TasksRunning = _tasksRunning;
            TasksWaitingForExecution = _tasksWaitingForExecution;
            TasksCompleted = _tasksCompleted;
            TasksFailed = _tasksFailed;
        }

        public int TasksRunning { get; }
        public int TasksWaitingForExecution { get; }
        public int TasksCompleted { get; }
        public int TasksFailed { get; }

    }
}
