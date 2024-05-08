using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.Build.Framework;

namespace VersionInfoGenerator.InternalTasks {
  [LoadInSeparateAppDomain]
  public class VIGGetGitInfo : MarshalByRefObject, ITask {
    private const string EVENT_CODE_GIT_REV_SHORT_PARSE_FAILED = "VIG1001";
    private const string EVENT_CODE_GIT_REV_LONG_PARSE_FAILED = "VIG1002";
    private const string EVENT_CODE_GIT_BRANCH_PARSE_FAILED = "VIG1003";
    private const string EVENT_CODE_GIT_COMMITS_SINCE_TAG_PARSE_FAILED = "VIG1004";
    private const string EVENT_CODE_LOCATE_GIT_FOLDER_FAILED = "VIG1005";

    public IBuildEngine BuildEngine { get; set; }

    public ITaskHost HostObject { get; set; }

    [Required]
    public string GitBinary { get; set; }

    [Output]
    public string GitRevShort { get; set; }

    [Output]
    public string GitRevLong { get; set; }

    [Output]
    public string GitBranch { get; set; }

    [Output]
    public string GitTag { get; set; }

    [Output]
    public int GitCommitsSinceTag { get; set; }

    [Output]
    public bool GitIsDirty { get; set; }

    public bool Execute() {
      var gitDir = RunTask(this.GetGitDir);
      using var mutex = MutexWrapper.FromPath("VIGGitInfo", gitDir);
      return RunTask(this.ExecuteAsync);
    }

    private static T RunTask<T>(Func<Task<T>> fn) {
      var task = fn();

      try {
        task.Wait();
      } catch (AggregateException ex) {
        foreach (var innerEx in ex.InnerExceptions) {
          ExceptionDispatchInfo.Capture(innerEx).Throw();
        }
      }

      return task.Result;
    }

    private async Task<string> GetGitDir() {
      const int MAX_ATTEMPTS = 10;
      var attempt = 0;
      while (true) {
        var (success, gitDir, err) = await this.TryRunGitCommand("rev-parse --git-dir");

        if (!success) {
          // "fatal: .git/index: index file open failed: Permission denied"
          if (err.ToLowerInvariant().Contains("permission denied") && attempt >= MAX_ATTEMPTS - 1) {
            await Task.Delay(100);
            attempt++;
            continue;
          }

          this.BuildEngine.LogErrorEvent(
            new(
              code: EVENT_CODE_LOCATE_GIT_FOLDER_FAILED,
              message: $"Failed to locate the .git folder: {err}",
              subcategory: null,
              file: null,
              lineNumber: 0,
              columnNumber: 0,
              endLineNumber: 0,
              endColumnNumber: 0,
              helpKeyword: null,
              senderName: nameof(VIGGetGitInfo)
            )
          );
          return null;
        }

        return gitDir;
      }
    }

    private async Task<bool> ExecuteAsync() {
      {
        var (success, value, err) = await this.TryRunGitCommand(
          "describe --long --always --dirty --exclude=* --abbrev=7"
        );

        if (!success) {
          this.BuildEngine.LogErrorEvent(
            new(
              code: EVENT_CODE_GIT_REV_SHORT_PARSE_FAILED,
              message: $"Failed to parse {nameof(this.GitRevShort)}: {err}",
              subcategory: null,
              file: null,
              lineNumber: 0,
              columnNumber: 0,
              endLineNumber: 0,
              endColumnNumber: 0,
              helpKeyword: null,
              senderName: nameof(VIGGetGitInfo)
            )
          );
          return false;
        }

        this.GitRevShort = value;
      }

      {
        var (success, value, err) = await this.TryRunGitCommand(
          "describe --long --always --dirty --exclude=* --abbrev=9999"
        );

        if (!success) {
          this.BuildEngine.LogErrorEvent(
            new(
              code: EVENT_CODE_GIT_REV_LONG_PARSE_FAILED,
              message: $"Failed to parse {nameof(this.GitRevLong)}: {err}",
              subcategory: null,
              file: null,
              lineNumber: 0,
              columnNumber: 0,
              endLineNumber: 0,
              endColumnNumber: 0,
              helpKeyword: null,
              senderName: nameof(VIGGetGitInfo)
            )
          );
          return false;
        }

        this.GitRevLong = value;
      }

      {
        var (success, value, err) = await this.TryRunGitCommand(
          "rev-parse --abbrev-ref --symbolic-full-name HEAD"
        );

        if (!success) {
          this.BuildEngine.LogErrorEvent(
            new(
              code: EVENT_CODE_GIT_BRANCH_PARSE_FAILED,
              message: $"Failed to parse {nameof(this.GitBranch)}: {err}",
              subcategory: null,
              file: null,
              lineNumber: 0,
              columnNumber: 0,
              endLineNumber: 0,
              endColumnNumber: 0,
              helpKeyword: null,
              senderName: nameof(VIGGetGitInfo)
            )
          );
          return false;
        }

        this.GitBranch = value;
      }

      {
        var (success, value, _) = await this.TryRunGitCommand("describe --tags --abbrev=0");
        if (success) {
          this.GitTag = value;
        }
      }

      if (!string.IsNullOrEmpty(this.GitTag)) {
        // XXX: this is probably gonna break if the tag contains quotes
        // and maybe spaces.
        // Unfortunately ProcessStartInfo.ArgumentList is not available
        // in netstandard2.0.
        var (success, value, err) = await this.TryRunGitCommand(
          $"rev-list \"{this.GitTag}\".. --count"
        );

        if (!success) {
          this.BuildEngine.LogErrorEvent(
            new(
              code: EVENT_CODE_GIT_COMMITS_SINCE_TAG_PARSE_FAILED,
              message: $"Failed to parse {nameof(this.GitCommitsSinceTag)}: {err}",
              subcategory: null,
              file: null,
              lineNumber: 0,
              columnNumber: 0,
              endLineNumber: 0,
              endColumnNumber: 0,
              helpKeyword: null,
              senderName: nameof(VIGGetGitInfo)
            )
          );
          return false;
        }

        if (!int.TryParse(value, out var number)) {
          this.BuildEngine.LogErrorEvent(
            new(
              code: EVENT_CODE_GIT_COMMITS_SINCE_TAG_PARSE_FAILED,
              message: $"Failed to parse {nameof(this.GitCommitsSinceTag)} value to int: {value}",
              subcategory: null,
              file: null,
              lineNumber: 0,
              columnNumber: 0,
              endLineNumber: 0,
              endColumnNumber: 0,
              helpKeyword: null,
              senderName: nameof(VIGGetGitInfo)
            )
          );
          return false;
        }

        this.GitCommitsSinceTag = number;
      }

      this.GitIsDirty = this.GitRevShort.Contains("-dirty");

      return true;
    }

    private async Task<(bool Success, string Output, string Error)> TryRunGitCommand(string args) {
      // XXX: ProcessStartInfo.ArgumentList would be nice to avoid
      // having to manually escape arguments, but it's not available
      // in netstandard2.0
      using var process = Process.Start(
        new ProcessStartInfo(this.GitBinary, args) {
          WindowStyle = ProcessWindowStyle.Hidden,
          CreateNoWindow = true,
          UseShellExecute = false,
          RedirectStandardInput = true,
          RedirectStandardError = true,
          RedirectStandardOutput = true,
        }
      );

      var stdOut = process.StandardOutput.ReadToEndAsync();
      var stdErr = process.StandardError.ReadToEndAsync();
      await Task.WhenAll(stdOut, stdErr);
      process.WaitForExit();

      return (process.ExitCode == 0, stdOut.Result.TrimEnd('\r', '\n'), stdErr.Result);
    }
  }
}
