using System;
using System.IO;

namespace VersionInfoGenerator.InternalTasks
{
  internal class PathUtils
  {
    public static string NormalizeAbsolute(string path) =>
      Uri.UnescapeDataString(new Uri(Path.GetFullPath(path)).AbsolutePath);
  }
}
