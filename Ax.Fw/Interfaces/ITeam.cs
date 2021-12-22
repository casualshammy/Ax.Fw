using Ax.Fw.Workers;
using System;

namespace Ax.Fw.Interfaces
{
    public interface ITeam<TJob>
    {
        IObservable<TJob> CompletedJobs { get; }
        TeamState State { get; }
    }
}
