namespace Ax.Fw.SharedTypes.Data.Workers;

public class JobResultCtx<T>
{
  public JobResultCtx(bool _success, T? _result, long _jobIndex)
  {
    Success = _success;
    Result = _result;
    JobIndex = _jobIndex;
  }

  public bool Success { get; }
  public T? Result { get; }
  public long JobIndex { get; }
}
