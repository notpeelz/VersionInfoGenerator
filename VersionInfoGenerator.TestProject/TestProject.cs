using System;

namespace VersionInfoGenerator.TestProject
{
    public class TestProject
    {
        public static void Main()
        {
            Console.WriteLine("RootNamespace: " + VersionInfoTest.RootNamespace);
            Console.WriteLine("Version: " + VersionInfoTest.Version);
            Console.WriteLine("VersionPrerelease: " + VersionInfoTest.VersionPrerelease);
            Console.WriteLine("VersionMetadata: " + VersionInfoTest.VersionMetadata);
            Console.WriteLine("SemVer: " + VersionInfoTest.SemVer);
            Console.WriteLine("GitRevShort: " + VersionInfoTest.GitRevShort);
            Console.WriteLine("GitRevLong: " + VersionInfoTest.GitRevLong);
            Console.WriteLine("GitBranch: " + VersionInfoTest.GitBranch);
            Console.WriteLine("GitTag: " + VersionInfoTest.GitTag);
            Console.WriteLine("GitIsDirty: " + VersionInfoTest.GitIsDirty);
        }
    }
}
