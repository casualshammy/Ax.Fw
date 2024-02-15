namespace Ax.Fw.DependencyInjection;

#if NET7_0_OR_GREATER
public interface IAppModule<T> where T : notnull
{
  public static abstract T ExportInstance(IAppDependencyCtx _ctx);
}
#endif