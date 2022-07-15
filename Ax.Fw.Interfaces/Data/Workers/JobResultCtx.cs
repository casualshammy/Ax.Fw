#nullable enable

namespace Ax.Fw.SharedTypes.Data.Workers
{
    public class JobResultCtx<T>
    {
        public JobResultCtx(T? _result, long _jobIndex)
        {
            Result = _result;
            JobIndex = _jobIndex;
        }

        public T? Result { get; }
        public long JobIndex { get; }
    }

}
