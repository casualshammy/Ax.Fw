using Ax.Fw.SharedTypes.Data.Workers;
using System;
using System.Threading.Tasks;

namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface ITeam<TJob, TJobResult>
    {
        IObservable<JobResultCtx<TJobResult?>> CompletedJobs { get; }
        TeamState State { get; }

        Task<TJobResult?> DoWork(TJob _job);
        void PostWork(TJob _job);
    }
}
