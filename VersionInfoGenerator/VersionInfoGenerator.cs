using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace VersionInfoGenerator {
  [Generator(LanguageNames.CSharp)]
  public class VersionInfoGenerator : IIncrementalGenerator {
    private abstract class Property { }

    private class LiteralExpressionProperty : Property {
      public delegate (LiteralExpressionSyntax expr, PredefinedTypeSyntax type) GetExpressionFunc(
        string? value
      );

      public LiteralExpressionProperty(GetExpressionFunc getExpressionFunc) {
        this.GetExpression = getExpressionFunc;
      }

      public GetExpressionFunc GetExpression { get; }
    }

    private class TokenProperty : Property {
      public delegate SyntaxToken GetTokenFunc(string value);

      public TokenProperty(GetTokenFunc getTokenFunc) {
        this.GetToken = getTokenFunc;
      }

      public GetTokenFunc GetToken { get; }
    }

    private static readonly LiteralExpressionProperty StringExpression =
      new(
        x =>
          (
            expr: x == null
              ? LiteralExpression(SyntaxKind.DefaultLiteralExpression)
              : LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(x)),
            type: PredefinedType(Token(SyntaxKind.StringKeyword))
          )
      );

    private static readonly LiteralExpressionProperty IntExpression =
      new(
        x =>
          (
            expr: x == null
              ? LiteralExpression(SyntaxKind.DefaultLiteralExpression)
              : LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(int.Parse(x))),
            type: PredefinedType(Token(SyntaxKind.IntKeyword))
          )
      );

    private static readonly LiteralExpressionProperty BoolExpression =
      new(
        x =>
          (
            expr: x == null
              ? LiteralExpression(SyntaxKind.DefaultLiteralExpression)
              : LiteralExpression(
                bool.Parse(x) ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression
              ),
            type: PredefinedType(Token(SyntaxKind.BoolKeyword))
          )
      );

    private static readonly TokenProperty ModifierToken =
      new(
        x => x switch {
          "private" => Token(SyntaxKind.PrivateKeyword),
          "protected" => Token(SyntaxKind.ProtectedKeyword),
          "internal" => Token(SyntaxKind.InternalKeyword),
          "public" => Token(SyntaxKind.PublicKeyword),
          "static" => Token(SyntaxKind.StaticKeyword),
          "abstract" => Token(SyntaxKind.AbstractKeyword),
          "partial" => Token(SyntaxKind.PartialKeyword),
          _ => throw new InvalidOperationException($"Unsupported modifier: {x}"),
        }
      );

#pragma warning disable RS2008
    private static readonly DiagnosticDescriptor VIG0001 =
      new(
        id: "VIG0001",
        title: "VersionInfoGenerator was run without the necessary MSBuild properties",
        messageFormat: "VersionInfoGenerator was run without the necessary MSBuild properties",
        category: "",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "https://github.com/notpeelz/VersionInfoGenerator/wiki/Diagnostic-messages#vig0001"
      );
    private static readonly Dictionary<string, Property> SerializableProperties = new() {
        { "RootNamespace", StringExpression },
        { "Version", StringExpression },
        { "VersionPrerelease", StringExpression },
        { "VersionMetadata", StringExpression },
        { "SemVer", StringExpression },
        { "GitRevShort", StringExpression },
        { "GitRevLong", StringExpression },
        { "GitBranch", StringExpression },
        { "GitCommitterDate", StringExpression },
        { "GitAuthorDate", StringExpression },
        { "GitTag", StringExpression },
        { "GitCommitsSinceTag", IntExpression },
        { "GitIsDirty", BoolExpression },
      };

    private class MSBuildProperties {
      private static readonly string[] DefaultSerializedProperties = new[] {
        nameof(RootNamespace),
        nameof(Version),
        nameof(VersionPrerelease),
        nameof(VersionMetadata),
        nameof(SemVer),
        nameof(GitRevShort),
        nameof(GitRevLong),
        nameof(GitBranch),
        nameof(GitCommitterDate),
        nameof(GitAuthorDate),
        nameof(GitTag),
        nameof(GitCommitsSinceTag),
        nameof(GitIsDirty),
      };

      public required string? VersionInfoGenerateClass { get; init; }

      public required string? BuildingProject { get; init; }

      public required string? RootNamespace { get; init; }

      public required string? VersionInfoClassNamespace { get; init; }

      public required string? VersionInfoClassNamespaceGlobal { get; init; }

      public required string? VersionInfoClassName { get; init; }

      public required string? VersionInfoClassModifiers { get; init; }

      public required Dictionary<string, string?> VersionInfoClassSerializedProperties { get; init; }

      public required string? Version { get; init; }

      public required string? VersionPrerelease { get; init; }

      public required string? VersionMetadata { get; init; }

      public required string? SemVer { get; init; }

      public required string? GitRevShort { get; init; }

      public required string? GitRevLong { get; init; }

      public required string? GitBranch { get; init; }

      public required string? GitCommitterDate { get; init; }

      public required string? GitAuthorDate { get; init; }

      public required string? GitTag { get; init; }

      public required string? GitCommitsSinceTag { get; init; }

      public required string? GitIsDirty { get; init; }

      private MSBuildProperties() { }

      public static MSBuildProperties FromAnalyzerOptions(AnalyzerConfigOptions globalOptions) {
        string? GetProperty(string name) {
          if (!globalOptions.TryGetValue($"build_property.{name}", out var value)) {
            return null;
          }

          if (string.IsNullOrEmpty(value)) {
            return null;
          }

          return value;
        }

        Dictionary<string, string?> GetSerializedProperties() {
          var encoded = GetProperty("_VersionInfoClassSerializedProperties");
          var decoded = encoded == null
            ? null
            : Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
          var propertyNames = decoded switch {
            null => DefaultSerializedProperties,
            var a => decoded
              .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
              .Select(x => x.Trim('\x20', '\r', '\n')),
          };
          return propertyNames
            .Select(x => new { Key = x, Value = GetProperty(x) })
            .ToDictionary(x => x.Key, x => x.Value);
        }

        return new MSBuildProperties {
          VersionInfoGenerateClass = GetProperty("VersionInfoGenerateClass"),
          BuildingProject = GetProperty("BuildingProject"),
          RootNamespace = GetProperty("RootNamespace"),
          VersionInfoClassNamespace = GetProperty("VersionInfoClassNamespace"),
          VersionInfoClassNamespaceGlobal = GetProperty("VersionInfoClassNamespaceGlobal"),
          VersionInfoClassName = GetProperty("VersionInfoClassName"),
          VersionInfoClassModifiers = GetProperty("VersionInfoClassModifiers"),
          VersionInfoClassSerializedProperties = GetSerializedProperties(),
          Version = GetProperty("Version"),
          VersionPrerelease = GetProperty("VersionPrerelease"),
          VersionMetadata = GetProperty("VersionMetadata"),
          SemVer = GetProperty("SemVer"),
          GitRevShort = GetProperty("GitRevShort"),
          GitRevLong = GetProperty("GitRevLong"),
          GitBranch = GetProperty("GitBranch"),
          GitCommitterDate = GetProperty("GitCommitterDate"),
          GitAuthorDate = GetProperty("GitAuthorDate"),
          GitTag = GetProperty("GitTag"),
          GitCommitsSinceTag = GetProperty("GitCommitsSinceTag"),
          GitIsDirty = GetProperty("GitIsDirty"),
        };
      }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context) {
      var propertiesProvider = context.AnalyzerConfigOptionsProvider
        .Select((options, _) => MSBuildProperties.FromAnalyzerOptions(options.GlobalOptions));

      context.RegisterSourceOutput(propertiesProvider, GenerateVersionInfoClass);
    }

    private static void GenerateVersionInfoClass(SourceProductionContext context, MSBuildProperties properties) {
      // XXX: we abort if this property is missing. This usually
      // happens when the consumer forgets to set PrivateAssets="all"
      // on the PackageReference.
      if (properties.VersionInfoGenerateClass == null) {
        context.ReportDiagnostic(Diagnostic.Create(VIG0001, null));
        return;
      }

      if (!bool.TryParse(properties.VersionInfoGenerateClass, out var generate)) {
        generate = true;
      }

      if (!generate) {
        return;
      }

      if (!bool.TryParse(properties.BuildingProject, out var buildingProject)) {
        buildingProject = false;
      }

      var rootNamespace = properties.RootNamespace;
      var classNamespace = properties.VersionInfoClassNamespace;
      if (
        !bool.TryParse(
          properties.VersionInfoClassNamespaceGlobal,
          out var useGlobalNamespace
        )
      ) {
        useGlobalNamespace = false;
      }

      if (useGlobalNamespace) {
        // VersionInfoClassNamespaceGlobal has precedence over
        // VersionInfoClassNamespace.
        classNamespace = null;
      } else {
        // If RootNamespace is null, it will fall back to the global
        // namespace.
        classNamespace = rootNamespace;
      }

      var className = properties.VersionInfoClassName ?? "VersionInfo";
      var modifiers = properties.VersionInfoClassModifiers ?? "internal static";

      var versionInfoClass = ClassDeclaration(className)
        .WithAttributeLists(
          List(
            new[] {
              AttributeList(
                SingletonSeparatedList(
                  Attribute(IdentifierName("GeneratedCode"))
                    .WithArgumentList(
                      AttributeArgumentList(
                        SeparatedList<AttributeArgumentSyntax>(
                          new SyntaxNodeOrToken[] {
                            AttributeArgument(
                              LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                Literal("VersionInfoGenerator")
                              )
                            ),
                            Token(SyntaxKind.CommaToken),
                            AttributeArgument(
                              LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                Literal(VersionInfo.SemVer)
                              )
                            ),
                          }
                        )
                      )
                    )
                )
              ),
              AttributeList(SingletonSeparatedList(Attribute(IdentifierName("CompilerGenerated")))),
            }
          )
        )
        .WithModifiers(
          TokenList(modifiers.Split(' ').Select(x => ModifierToken.GetToken(x)).ToArray())
        )
        .WithMembers(List(GenerateFields().Cast<MemberDeclarationSyntax>().ToArray()));

      var src = CompilationUnit()
        .WithUsings(
          List(
            new[] {
              UsingDirective(
                QualifiedName(
                  QualifiedName(IdentifierName("System"), IdentifierName("CodeDom")),
                  IdentifierName("Compiler")
                )
              ),
              UsingDirective(
                QualifiedName(
                  QualifiedName(IdentifierName("System"), IdentifierName("Runtime")),
                  IdentifierName("CompilerServices")
                )
              ),
            }
          )
        )
        .WithMembers(
          classNamespace != null
            ? SingletonList<MemberDeclarationSyntax>(
              NamespaceDeclaration(IdentifierName(classNamespace))
                .WithMembers(SingletonList<MemberDeclarationSyntax>(versionInfoClass))
            )
            // Use the global namespace if we don't have a namespace
            : SingletonList<MemberDeclarationSyntax>(versionInfoClass)
        )
        .NormalizeWhitespace();

      context.AddSource(
        hintName: classNamespace == null ? className : $"{classNamespace}.{className}",
        sourceText: SourceText.From(src.ToFullString(), Encoding.UTF8)
      );

      IEnumerable<FieldDeclarationSyntax> GenerateFields() {
        var props = properties.VersionInfoClassSerializedProperties;
        foreach (var prop in SerializableProperties) {
          if (!props.TryGetValue(prop.Key, out var value)) {
            continue;
          }

          if (prop.Value is not LiteralExpressionProperty literalProp) {
            throw new InvalidOperationException(
              $"Unsupported property value type: {prop.GetType()}"
            );
          }

          // If the project is building, don't populate any fields
          // since the GitInfo properties haven't been populated yet.
          var (expr, type) = literalProp.GetExpression(buildingProject ? value : null);

          yield return FieldDeclaration(
              VariableDeclaration(type)
                .WithVariables(
                  SingletonSeparatedList(
                    VariableDeclarator(Identifier(prop.Key))
                      .WithInitializer(EqualsValueClause(expr))
                  )
                )
            )
            .WithModifiers(
              TokenList(new[] { Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.ConstKeyword), })
            );
        }
      }
    }
  }
}
