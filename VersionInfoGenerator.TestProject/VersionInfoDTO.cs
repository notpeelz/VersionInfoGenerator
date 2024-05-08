public class VersionInfoDTO {
  public string RootNamespace { get; set; }

  public string Version { get; set; }

  public string VersionPrerelease { get; set; }

  public string VersionMetadata { get; set; }

  public string SemVer { get; set; }

  public string GitRevShort { get; set; }

  public string GitRevLong { get; set; }

  public string GitBranch { get; set; }

  public string GitTag { get; set; }

  public int GitCommitsSinceTag { get; set; }

  public bool GitIsDirty { get; set; }
}
