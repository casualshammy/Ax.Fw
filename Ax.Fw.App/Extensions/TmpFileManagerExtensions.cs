using Ax.Fw.App.Interfaces;
using Ax.Fw.App.Modules.TmpFileManager;
using Ax.Fw.DependencyInjection;
using Ax.Fw.SharedTypes.Interfaces;

namespace Ax.Fw.App.Extensions;

public static class TmpFileManagerExtensions
{
  /// <summary>
  /// Registers a singleton <see cref="ITmpFileManager"/> service in the application's dependency container, using a
  /// dynamic observable to determine the temporary directory path.
  /// </summary>
  /// <remarks>The registered <see cref="ITmpFileManager"/> will use the provided observable to track changes to
  /// the temporary directory path at runtime. This enables dynamic reconfiguration of the temporary file location while
  /// the application is running.</remarks>
  /// <param name="_appBase">The <see cref="AppBase"/> instance to configure.</param>
  /// <param name="_tmpDirFlowFunc">A function that, given the current dependency context, returns an observable sequence of temporary directory paths
  /// to be used by the file manager. The observable may emit <c>null</c> to indicate that system default temporary directory must be used.</param>
  /// <returns>The <see cref="AppBase"/> instance, to allow for method chaining.</returns>
  public static AppBase UseTmpFileManager(
    this AppBase _appBase,
    Func<IAppDependencyCtx, IObservable<string?>> _tmpDirFlowFunc)
  {
    return _appBase.AddSingleton<ITmpFileManager>(_ctx =>
    {
      var log = _ctx.Locate<ILog>();
      var lifetime = _ctx.Locate<IReadOnlyLifetime>();
      var tmpDirFlow = _tmpDirFlowFunc(_ctx);

      var tmpFileMgr = new TmpFileManagerImpl(
        log["tmp-file-mgr"],
        tmpDirFlow,
        lifetime);

      return tmpFileMgr;
    });
  }

  /// <summary>
  /// Registers a singleton <see cref="ITmpFileManager"/> service in the application's dependency container, using the specified
  /// observable for the temporary directory path.
  /// </summary>
  /// <remarks>This method adds an <see cref="ITmpFileManager"/> implementation as a singleton service to the
  /// application's dependency manager. The temporary file manager uses the provided observable to determine the
  /// directory for storing temporary files. If the observable emits a new value, the temporary file manager will update
  /// its target directory accordingly.</remarks>
  /// <param name="_appBase">The application base instance to which the temporary file manager will be added.</param>
  /// <param name="_tmpDirFlow">An observable sequence that provides the path to the temporary directory. The observable may emit <see
  /// langword="null"/> to indicate that system default temporary directory must be used.</param>
  /// <returns>The original <paramref name="_appBase"/> instance, enabling method chaining.</returns>
  public static AppBase UseTmpFileManager(
    this AppBase _appBase,
    IObservable<string?> _tmpDirFlow)
    => _appBase.UseTmpFileManager(_ => _tmpDirFlow);

}
