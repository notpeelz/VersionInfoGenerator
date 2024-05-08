# VersionInfoGenerator

[![Latest version](https://img.shields.io/nuget/v/VersionInfoGenerator?color=green&style=for-the-badge)](https://www.nuget.org/packages/VersionInfoGenerator)
[![License](https://img.shields.io/badge/license-MIT-blue?style=for-the-badge)](LICENSE)

A .NET library that makes csproj version information (including git state) available at runtime and compile time.

Compliant with [SemVer 2.0.0](https://semver.org)

# How to use

In your `.csproj`:

```xml
<PropertyGroup>
  <Version>1.0.0</Version> <!-- Major.Minor.Patch -->
  <VersionPrerelease>rc1</VersionPrerelease> <!-- (Optional) for prereleases: 1.0.0-rc1 -->
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="VersionInfoGenerator" Version="3.0.0" PrivateAssets="all" />
</ItemGroup>
```

Output:

![file_info](https://raw.githubusercontent.com/notpeelz/VersionInfoGenerator/fe6d8c6461b1c7dfc86dcd850d5e843e2d840cbb/img/file_info.jpg)

```cs
internal static class VersionInfo
{
    public const string RootNamespace = "VersionInfoGenerator.TestProject";
    public const string Version = "1.0.0";
    public const string VersionPrerelease = "rc1";
    public const string VersionMetadata = "git-ac717b1";
    public const string SemVer = "1.0.0-rc1+git-ac717b1";
    public const string GitRevShort = "ac717b1";
    public const string GitRevLong = "ac717b1885cd0f984cabe77dd5f37c9200795298";
    public const string GitBranch = "master";
    public const string GitTag = "v1.0.0";
    public const int GitCommitsSinceTag = 0;
    public const bool GitIsDirty = false;
}
```

# Settings

```xml
<PropertyGroup>
  <!-- Major.Minor.Patch -->
  <Version>1.0.0</Version>
  <!-- The SemVer prerelease tag -->
  <VersionPrerelease></VersionPrerelease>
  <!-- If specified, it replaces the default SemVer metadata format -->
  <VersionMetadata>git-@@GitRevShort@@</VersionMetadata>
  <!-- Controls whether the SemVer should have a metadata component -->
  <VersionInfoUseMetadata>true</VersionInfoUseMetadata>
  <!-- Controls whether to override FileVersion and InformationalVersion -->
  <VersionInfoOverrideVersions>true</VersionInfoOverrideVersions>
  <!-- Controls whether to override PackageVersion -->
  <VersionInfoOverridePackageVersion>true</VersionInfoOverridePackageVersion>
  <!-- Controls whether to override RepositoryCommit and RepositoryBranch -->
  <VersionInfoOverrideRepositoryInfo>true</VersionInfoOverrideRepositoryInfo>
  <!-- The name of generated class -->
  <VersionInfoClassName>VersionInfo</VersionInfoClassName>
  <!-- The namespace to use for the VersionInfo class -->
  <!-- (uses the value of <RootNamespace> as fallback) -->
  <VersionInfoClassNamespace></VersionInfoClassNamespace>
  <!-- Controls whether to use the global namespace for the VersionInfo class -->
  <VersionInfoClassNamespaceGlobal>false</VersionInfoClassNamespaceGlobal>
  <!-- The modifiers of the VersionInfo class -->
  <VersionInfoClassModifiers>internal static</VersionInfoClassModifiers>
  <!-- Controls whether to generate the VersionInfo class -->
  <VersionInfoGenerateClass>true</VersionInfoGenerateClass>
  <!-- Controls what properties to include in the VersionInfo class -->
  <VersionInfoClassSerializedProperties>RootNamespace;Version;VersionPrerelease;VersionMetadata;SemVer;GitRevShort;GitRevLong;GitBranch;GitTag;GitCommitsSinceTag;GitIsDirty</VersionInfoClassSerializedProperties>
  <!-- Controls whether to generate a VersionInfo JSON file in the output folder -->
  <VersionInfoGenerateJson>false</VersionInfoGenerateJson>
  <!-- The name of the VersionInfo JSON file -->
  <VersionInfoJsonOutputPath>VersionInfo.json</VersionInfoJsonOutputPath>
  <!-- Controls what properties to include in the VersionInfo JSON file -->
  <VersionInfoJsonSerializedProperties>RootNamespace;Version;VersionPrerelease;VersionMetadata;SemVer;GitRevShort;GitRevLong;GitBranch;GitTag;GitCommitsSinceTag;GitIsDirty</VersionInfoClassJsonSerializedProperties>
</PropertyGroup>
```

A config file (named `VersionInfoGenerator.Config.props`) can be created to gain more control over the MSBuild properties, e.g.:

```xml
<Project>
  <PropertyGroup>
    <GitBinary>/path/to/git</GitBinary>
  </PropertyGroup>

  <Target Name="VersionInfoConfig" AfterTargets="VersionInfoGenerator_GetGitInfo">
    <PropertyGroup>
      <VersionMetadata>NO_TAG</VersionMetadata>
      <VersionMetadata Condition="'$(GitTag)' != ''">$(GitTag)</VersionMetadata>
    </PropertyGroup>

    <PropertyGroup Condition="'$(GitCommitsSinceTag)' != '0'">
      <VersionMetadata>$(VersionMetadata)-$(GitCommitsSinceTag)</VersionMetadata>
    </PropertyGroup>

    <PropertyGroup Condition="'$(GitBranch)' == 'master'">
      <VersionMetadata>$(VersionMetadata)-RELEASE</VersionMetadata>
    </PropertyGroup>
  </Target>
</Project>
```

## VersionInfo.json

A JSON file can be generated for version processing by external tools, e.g.:

```xml
<PropertyGroup>
  <VersionInfoGenerateJson>true</VersionInfoGenerateJson>
</PropertyGroup>
```

Output (`bin/Release/xxx/VersionInfo.json`):

```json
{
  "RootNamespace": "VersionInfoGenerator.TestProject",
  "Version": "1.0.0",
  "VersionPrerelease": null,
  "VersionMetadata": "git-0378e47",
  "SemVer": "1.0.0+git-0378e47",
  "GitRevShort": "0378e47",
  "GitRevLong": "0378e47109d698eeceaf07ad75e48ea36143d2e3",
  "GitBranch": "master",
  "GitTag": "v1.0.0",
  "GitCommitsSinceTag": 0,
  "GitIsDirty": false
}
```

## Special variable substitution

Special variables can be used to customize the SemVer metadata (as an alternative to `VersionInfoGenerator.Config.props`):

- `@@GitRevShort@@`: the 7-character hash of the current commit (suffixed with `-dirty` if there's uncommited changes)
- `@@GitRevLong@@`: the full hash of the current commit (suffixed with `-dirty` if there's uncommited changes)
- `@@GitBranch@@`: the current git branch
- `@@GitTag@@`: the current git tag
- `@@GitCommitsSinceTag@@`: the number of commits since the last git tag
- `@@VersionMetadata@`: the default VersionMetadata format (`git-@@GitRevShort@@`)

# Troubleshooting

If you encounter any issues, make sure to:

- Rebuild the project/solution then restart your IDE
- Test building through the command line using `dotnet build`
- Update your IDE to the latest version (requires at least Visual Studio 2019 16.9; Jetbrains Rider is untested)
- Update the dotnet runtime to the latest version

If none of the above works, [open an issue](https://github.com/notpeelz/VersionInfoGenerator/issues/new).
