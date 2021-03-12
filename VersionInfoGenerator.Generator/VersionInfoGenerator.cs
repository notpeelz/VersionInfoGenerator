using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VersionInfoGenerator.Generator
{
    [Generator]
    public class VersionInfoGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        private static readonly string[] PropertyNames = new[]
        {
            "RootNamespace",
            "Version",
            "VersionPrerelease",
            "VersionMetadata",
            "SemVer",
            "GitRevShort",
            "GitRevLong",
            "GitBranch",
        };

        public void Execute(GeneratorExecutionContext context)
        {
            if (!bool.TryParse(GetProperty("GenerateVersionInfo"), out var shouldGenerate))
            {
                shouldGenerate = true;
            }
            if (!shouldGenerate) return;

            var rootNs = GetProperty("RootNamespace");
            var className = GetProperty("VersionInfoClassName") ?? "VersionInfo";
            var accessModifier = GetProperty("VersionInfoClassAccessibilityModifier") ?? "internal";
            var hintName = rootNs == null ? className : $"{rootNs}.{className}";

            var src = $@"
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

{(rootNs == null ? "// namespace <global> {" : $"namespace {rootNs} {{")}
    [GeneratedCode(""{hintName}"", ""0.1"")]
    [CompilerGenerated]
    {accessModifier} static class {className} {{
{string.Join("\n", GenerateFields().Select(x => new string(' ', 8) + x))}
    }}
{(rootNs == null ? "// }" : "}")}
";

            context.AddSource(hintName, SourceText.From(src, Encoding.UTF8));

            IEnumerable<string> GenerateFields()
            {
                foreach (var name in PropertyNames)
                {
                    yield return $"public const string {name} = \"{GetProperty(name)}\";";
                }
            }

            string GetProperty(string name)
            {
                if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{name}", out var value))
                {
                    throw new InvalidOperationException($"Missing MSBuild property: {name}");
                }

                if (string.IsNullOrEmpty(value)) return null;
                return value.Replace("\"", "\\\"");
            }
        }
    }
}
