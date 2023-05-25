using System;
using System.IO;
using System.Text.Json;

Console.WriteLine("Class:");
Console.WriteLine("------------------------");
Console.WriteLine("RootNamespace: " + VersionInfoTest.RootNamespace);
Console.WriteLine("Version: " + VersionInfoTest.Version);
Console.WriteLine("VersionPrerelease: " + VersionInfoTest.VersionPrerelease);
Console.WriteLine("VersionMetadata: " + VersionInfoTest.VersionMetadata);
Console.WriteLine("SemVer: " + VersionInfoTest.SemVer);
Console.WriteLine("GitRevShort: " + VersionInfoTest.GitRevShort);
Console.WriteLine("GitRevLong: " + VersionInfoTest.GitRevLong);
Console.WriteLine("GitBranch: " + VersionInfoTest.GitBranch);
Console.WriteLine("GitTag: " + VersionInfoTest.GitTag);
Console.WriteLine("GitCommitsSinceTag: " + VersionInfoTest.GitCommitsSinceTag);
Console.WriteLine("GitIsDirty: " + VersionInfoTest.GitIsDirty);

Console.WriteLine();

var dir = System.IO.Path.GetDirectoryName(
  System.Reflection.Assembly.GetExecutingAssembly().Location
);
using var sr = new StreamReader(Path.Combine(dir, "VersionInfo.json"));
var json = JsonSerializer.Deserialize<VersionInfoDTO>(sr.ReadToEnd());

Console.WriteLine("JSON:");
Console.WriteLine("------------------------");
Console.WriteLine("RootNamespace: " + json.RootNamespace);
Console.WriteLine("Version: " + json.Version);
Console.WriteLine("VersionPrerelease: " + json.VersionPrerelease);
Console.WriteLine("VersionMetadata: " + json.VersionMetadata);
Console.WriteLine("SemVer: " + json.SemVer);
Console.WriteLine("GitRevShort: " + json.GitRevShort);
Console.WriteLine("GitRevLong: " + json.GitRevLong);
Console.WriteLine("GitBranch: " + json.GitBranch);
Console.WriteLine("GitTag: " + json.GitTag);
Console.WriteLine("GitCommitsSinceTag: " + json.GitCommitsSinceTag);
Console.WriteLine("GitIsDirty: " + json.GitIsDirty);
