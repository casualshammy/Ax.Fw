namespace Ax.Fw;

public class ProcessEventData
{
  public ProcessEventData(int _processId, string _processName)
  {
    ProcessId = _processId;
    ProcessName = _processName;
  }

  public int ProcessId { get; }
  public string ProcessName { get; }

}
