using Ax.Fw.App.Interfaces;
using Ax.Fw.App.Modules.TmpFileManager;
using Ax.Fw.SharedTypes.Interfaces;

namespace Ax.Fw.App.Extensions;

public static class TmpFileManagerExtensions
{
  /// <summary>
  /// Registers a temporary file manager service in the application's dependency container, using the specified
  /// observable for the temporary directory path.
  /// </summary>
  /// <remarks>This method adds an <see cref="ITmpFileManager"/> implementation as a singleton service to the
  /// application's dependency manager. The temporary file manager uses the provided observable to determine the
  /// directory for storing temporary files. If the observable emits a new value, the temporary file manager will update
  /// its target directory accordingly.</remarks>
  /// <param name="_appBase">The application base instance to which the temporary file manager will be added. Must not be <c>null</c>.</param>
  /// <param name="_tmpDirFlow">An observable sequence that provides the path to the temporary directory. The observable may emit <see
  /// langword="null"/> to indicate that system default temporary directory must be used.</param>
  /// <returns>The original <paramref name="_appBase"/> instance, enabling method chaining.</returns>
  public static AppBase UseTmpFileManager(
    this AppBase _appBase,
    IObservable<string?> _tmpDirFlow)
  {
    return _appBase.AddSingleton<ITmpFileManager>(_ctx =>
    {
      var log = _ctx.Locate<ILog>();
      var lifetime = _ctx.Locate<IReadOnlyLifetime>();

      var tmpFileMgr = new TmpFileManagerImpl(
        log["tmp-file-mgr"],
        _tmpDirFlow,
        lifetime);

      return tmpFileMgr;
    });
  }
}
