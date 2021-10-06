using System;
using System.Threading.Tasks;

namespace Ax.Fw.Interfaces
{
    public interface IJsonStorage<T>
    {
        string JsonFilePath { get; }

        T Load(Func<T> _defaultFactory);
        Task<T> LoadAsync(Func<Task<T>> _defaultFactory);
        void Save(T data, bool humanReadable = false);
    }
}