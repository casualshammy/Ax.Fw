using System;
using System.Threading.Tasks;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface ILifetime : IReadOnlyLifetime, IDisposable
{
    void End();
}
