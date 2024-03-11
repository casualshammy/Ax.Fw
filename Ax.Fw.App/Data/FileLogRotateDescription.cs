using System.Text.RegularExpressions;

namespace Ax.Fw.App.Data;

public record FileLogRotateDescription(DirectoryInfo Directory, bool Recursive, Regex LogFilesPattern, TimeSpan LogFileTtl);
