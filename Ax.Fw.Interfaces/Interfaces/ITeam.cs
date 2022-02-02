using Ax.Fw.SharedTypes.Data;
using System;

namespace Ax.Fw.SharedTypes.Interfaces
{
    public interface ITeam<TJob>
    {
        IObservable<TJob> CompletedJobs { get; }
        TeamState State { get; }
    }
}
