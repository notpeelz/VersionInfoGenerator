using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;

namespace VersionInfoGenerator.JsonTask
{
  public class CreateVersionInfoJson : ITask
  {
    public IBuildEngine BuildEngine { get; set; }

    public ITaskHost HostObject { get; set; }

    [Output]
    public string[] GeneratedFiles { get; set; }

    [Required]
    public string MSBuildOutputPath { get; set; }

    public string OutputPath { get; set; }

    public string SerializedProperties { get; set; }

    #region Serialized properties
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
    #endregion

    public bool Execute()
    {
      // Make sure that we're not accidentally creating the build dir
      // at the wrong step in the MSBuild pipeline.
      if (!Directory.Exists(this.MSBuildOutputPath))
      {
        this.BuildEngine.LogErrorEvent(
          new(
            code: "VIGJ0001",
            message: $"The MSBuild output path doesn't exist: {this.MSBuildOutputPath}",
            subcategory: null,
            file: null,
            lineNumber: 0,
            columnNumber: 0,
            endLineNumber: 0,
            endColumnNumber: 0,
            helpKeyword: null,
            senderName: nameof(CreateVersionInfoJson)
          )
        );
        return false;
      }

      this.OutputPath = this.OutputPath?.Replace('\\', '/');
      var path = Path.Combine(this.MSBuildOutputPath, this.OutputPath ?? "");

      // Make sure we're not about to write to files outside of the
      // build dir.
      if (!Path.GetFullPath(path).StartsWith(Path.GetFullPath(this.MSBuildOutputPath)))
      {
        this.BuildEngine.LogErrorEvent(
          new(
            code: "VIGJ0002",
            message: "OutputPath has to stay within the MSBuild output path.",
            subcategory: null,
            file: null,
            lineNumber: 0,
            columnNumber: 0,
            endLineNumber: 0,
            endColumnNumber: 0,
            helpKeyword: null,
            senderName: nameof(CreateVersionInfoJson)
          )
        );
        return false;
      }

      // If OutputPath is null or ends with "/", we append the
      // default filename.
      if (this.OutputPath == null || this.OutputPath.EndsWith("/"))
      {
        path = Path.Combine(path, "VersionInfo.json");
      }

      // Create subfolders
      Directory.CreateDirectory(Path.GetDirectoryName(path));

      var props = this.SerializedProperties
        ?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Trim('\x20', '\r', '\n'))
        .ToArray();

      using var sw = new StreamWriter(path, false, Encoding.UTF8);
      var obj = new VersionInfoDTO
      {
        RootNamespace = this.RootNamespace,
        Version = this.Version,
        VersionPrerelease = this.VersionPrerelease,
        VersionMetadata = this.VersionMetadata,
        SemVer = this.SemVer,
        GitRevShort = this.GitRevShort,
        GitRevLong = this.GitRevLong,
        GitBranch = this.GitBranch,
        GitTag = this.GitTag,
        GitIsDirty = this.GitIsDirty,
      };
      sw.Write(SimpleJson.SimpleJson.SerializeObject(obj));
      this.GeneratedFiles = new[] { path };
      return true;
    }
  }
}
