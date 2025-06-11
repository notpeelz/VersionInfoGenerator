#pragma warning disable IDE0130

#if !NET5_0_OR_GREATER
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices {
  [ExcludeFromCodeCoverage, DebuggerNonUserCode]
  [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
  public sealed class CompilerFeatureRequiredAttribute : Attribute {
    public CompilerFeatureRequiredAttribute(string featureName) {
      this.FeatureName = featureName;
    }

    public string FeatureName { get; }

    public bool IsOptional { get; init; }

    public const string RefStructs = nameof(RefStructs);

    public const string RequiredMembers = nameof(RequiredMembers);
  }
}
#else
using System.Runtime.CompilerServices;

[assembly:TypeForwardedTo(typeof(CompilerFeatureRequired))]
#endif
