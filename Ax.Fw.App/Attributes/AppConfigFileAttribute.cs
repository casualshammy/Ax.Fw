namespace Ax.Fw.App.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AppConfigFileAttribute : Attribute
{
  public AppConfigFileAttribute(string _filePath)
  {
    FilePath = _filePath;
  }

  public string FilePath { get; }

}
