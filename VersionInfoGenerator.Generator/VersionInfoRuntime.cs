namespace VersionInfoGenerator.Generator
{
    internal static class VersionInfoRuntime
    {
        static VersionInfoRuntime()
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            var @class = asm.GetType("VersionInfoGenerator.VersionInfo");
            if (@class != null)
            {
                RootNamespace = (string)@class.GetField("RootNamespace").GetValue(null);
                SemVer = (string)@class.GetField("SemVer").GetValue(null);
            }
        }

        public static string RootNamespace { get; private set; } = "VersionInfoGenerator";

        public static string SemVer { get; private set; } = "0.0.0";
    }
}
