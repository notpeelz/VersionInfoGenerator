#pragma warning disable IDE0130

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices {
  using global::System.Diagnostics;
  using global::System.Diagnostics.CodeAnalysis;

  [ExcludeFromCodeCoverage, DebuggerNonUserCode]
  internal static class IsExternalInit { }
}
#else
using System.Runtime.CompilerServices;

[assembly:TypeForwardedTo(typeof(IsExternalInit))]
#endif
