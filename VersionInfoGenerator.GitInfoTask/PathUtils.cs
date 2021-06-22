using System;
using System.IO;

namespace VersionInfoGenerator.GitInfoTask
{
    internal class PathUtils
    {
        public static string NormalizeAbsolute(string path) =>
            Uri.UnescapeDataString(new Uri(Path.GetFullPath(path)).AbsolutePath);
    }
}
