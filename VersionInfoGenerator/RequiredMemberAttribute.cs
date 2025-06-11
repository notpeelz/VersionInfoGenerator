#pragma warning disable IDE0130

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices {
  using global::System.Diagnostics;
  using global::System.Diagnostics.CodeAnalysis;

  [ExcludeFromCodeCoverage, DebuggerNonUserCode]
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
  internal class RequiredMemberAttribute : Attribute {
    public RequiredMemberAttribute() { }
  }
}
#else
using System.Runtime.CompilerServices;

[assembly:TypeForwardedTo(typeof(RequiredMemberAttribute))]
#endif
