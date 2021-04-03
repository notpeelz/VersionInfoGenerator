using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Build.Framework;

namespace VersionInfoGenerator.JsonTask
{
    // For some reason, LoadInSeparateAppDomainAttribute isn't sufficient to
    // prevent MSBuild from locking the task DLL.
    // This is a problem because the NodeReuse MSBuild feature keeps a
    // background instance of MSBuild running, preventing the Task DLL from
    // being rebuilt.
    // As a workaround, NodeReuse can be disabled by setting
    // $Env:MSBUILDDISABLENODEREUSE=1
    public class VersionInfoGeneratorJsonTask : ITask
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

        public bool GitIsDirty { get; set; }
        #endregion

        public bool Execute()
        {
            // Make sure that we're not accidentally creating the build dir
            // at the wrong step in the MSBuild pipeline.
            if (!Directory.Exists(this.MSBuildOutputPath))
            {
                this.BuildEngine.LogErrorEvent(new(null, "VIG0001", null, 0, 0, 0, 0, $"MSBuildOutputPath doesn't exist: {this.MSBuildOutputPath}", "", nameof(VersionInfoGeneratorJsonTask)));
                return false;
            }

            this.OutputPath = this.OutputPath?.TrimStart('/');
            var path = Path.Combine(this.MSBuildOutputPath, this.OutputPath ?? "");

            // Make sure we're not about to write to files outside of the
            // build dir.
            if (!Path.GetFullPath(path).StartsWith(Path.GetFullPath(this.MSBuildOutputPath)))
            {
                this.BuildEngine.LogErrorEvent(new(null, "VIG0002", null, 0, 0, 0, 0, "OutputPath has to stay within MSBuildOutputPath.", "", nameof(VersionInfoGeneratorJsonTask)));
                return false;
            }

            // If OutputPath is null or ends with "/", we append the
            // default filename.
            if (this.OutputPath?.EndsWith("/") != false)
            {
                path = Path.Combine(path, "VersionInfo.json");
            }

            // Create subfolders
            // Note: MSBuildOutputPath should already exist since we check
            // for that at the beginning.
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var props = this.SerializedProperties?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim('\x20', '\r', '\n'))
                .ToArray();

            using var sw = new StreamWriter(path);
            using var ms = new MemoryStream();
            using (var jsonWriter = new Utf8JsonWriter(ms, new() { Indented = true }))
            {
                jsonWriter.WriteStartObject();
                SerializeProperty(nameof(this.RootNamespace), this.RootNamespace);
                SerializeProperty(nameof(this.Version), this.Version);
                SerializeProperty(nameof(this.VersionPrerelease), this.VersionPrerelease);
                SerializeProperty(nameof(this.VersionMetadata), this.VersionMetadata);
                SerializeProperty(nameof(this.SemVer), this.SemVer);
                SerializeProperty(nameof(this.GitRevShort), this.GitRevShort);
                SerializeProperty(nameof(this.GitRevLong), this.GitRevLong);
                SerializeProperty(nameof(this.GitBranch), this.GitBranch);
                SerializeProperty(nameof(this.GitTag), this.GitTag);
                SerializeProperty(nameof(this.GitIsDirty), this.GitIsDirty);
                jsonWriter.WriteEndObject();

                void SerializeProperty<T>(string name, T value)
                {
                    if (props != null && props.Length > 0 && !props.Contains(name)) return;

                    if (typeof(T) == typeof(string))
                        jsonWriter.WriteString(name, (string)(object)value);
                    else if (typeof(T) == typeof(bool))
                        jsonWriter.WriteBoolean(name, (bool)(object)value);
                    else
                        throw new InvalidOperationException($"Unsupported property type: {typeof(T)}");
                }
            }

            sw.Write(Encoding.UTF8.GetString(ms.ToArray()));
            this.GeneratedFiles = new[] { path };
            return true;
        }
    }
}
