using Ax.Fw.SharedTypes.Data.Workers;
using System;
using System.Threading.Tasks;

namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface ITeam<TJob, TJobResult>
    {
        IObservable<JobResultCtx<TJobResult?>> SuccessfullyCompletedJobs { get; }
        TeamState State { get; }
    IObservable<JobResultCtx<TJobResult?>> AllCompletedJobs { get; }

    Task<TJobResult?> DoWork(TJob _job);
        void PostWork(TJob _job);
    }
}
