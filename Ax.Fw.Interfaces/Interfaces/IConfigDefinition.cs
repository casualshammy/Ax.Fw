using System.Text.Json.Serialization;

namespace Ax.Fw.SharedTypes.Interfaces;

public interface IConfigDefinition
{
  public static abstract string FilePath { get; }
  public static abstract JsonSerializerContext? JsonCtx { get; }
}
