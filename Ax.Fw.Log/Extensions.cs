using Ax.Fw.SharedTypes.Interfaces;

namespace Ax.Fw.Log;

public static class Extensions
{
  public static NamedLogger GetNamedLog(this ILogger _logger, string _name) => new(_logger, _name);
}
