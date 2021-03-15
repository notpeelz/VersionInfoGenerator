using System;

namespace VersionInfoGenerator.TestProject
{
    public class TestProject
    {
        public static void Main()
        {
            Console.WriteLine("RootNamespace: " + VersionInfo.RootNamespace);
            Console.WriteLine("Version: " + VersionInfo.Version);
            Console.WriteLine("VersionPrerelease: " + VersionInfo.VersionPrerelease);
            Console.WriteLine("VersionMetadata: " + VersionInfo.VersionMetadata);
            Console.WriteLine("SemVer: " + VersionInfo.SemVer);
            Console.WriteLine("GitRevShort: " + VersionInfo.GitRevShort);
            Console.WriteLine("GitRevLong: " + VersionInfo.GitRevLong);
            Console.WriteLine("GitBranch: " + VersionInfo.GitBranch);
            Console.WriteLine("GitTag: " + VersionInfo.GitTag);
        }
    }
}
