using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace VersionInfoGenerator
{
    [Generator]
    public class VersionInfoGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        private abstract class Property { }

        private class LiteralExpressionProperty : Property
        {
            public delegate (LiteralExpressionSyntax expr, PredefinedTypeSyntax type) GetExpressionFunc(string value);

            public LiteralExpressionProperty(GetExpressionFunc getExpressionFunc)
            {
                this.GetExpression = getExpressionFunc;
            }

            public GetExpressionFunc GetExpression { get; }
        }

        private class TokenProperty : Property
        {
            public delegate SyntaxToken GetTokenFunc(string value);

            public TokenProperty(GetTokenFunc getTokenFunc)
            {
                this.GetToken = getTokenFunc;
            }

            public GetTokenFunc GetToken { get; }
        }

        private static readonly LiteralExpressionProperty StringExpression = new(x => (
            expr: x == null
                ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                : LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(x)),
            type: PredefinedType(Token(SyntaxKind.StringKeyword))));

        private static readonly LiteralExpressionProperty BoolExpression = new(x => (
            expr: LiteralExpression(
                (bool)Convert.ChangeType(x ?? "false", TypeCode.Boolean)
                    ? SyntaxKind.TrueLiteralExpression
                    : SyntaxKind.FalseLiteralExpression),
            type: PredefinedType(Token(SyntaxKind.BoolKeyword))));

        private static readonly TokenProperty ModifierToken = new(x =>
            x switch
            {
                "private" => Token(SyntaxKind.PrivateKeyword),
                "protected" => Token(SyntaxKind.ProtectedKeyword),
                "internal" => Token(SyntaxKind.InternalKeyword),
                "public" => Token(SyntaxKind.PublicKeyword),
                "static" => Token(SyntaxKind.StaticKeyword),
                "abstract" => Token(SyntaxKind.AbstractKeyword),
                "partial" => Token(SyntaxKind.PartialKeyword),
                _ => throw new InvalidOperationException($"Unsupported modifier: {x}"),
            });

        private static readonly Dictionary<string, Property> SerializedProperties = new()
        {
            { "RootNamespace", StringExpression },
            { "Version", StringExpression },
            { "VersionPrerelease", StringExpression },
            { "VersionMetadata", StringExpression },
            { "SemVer", StringExpression },
            { "GitRevShort", StringExpression },
            { "GitRevLong", StringExpression },
            { "GitBranch", StringExpression },
            { "GitTag", StringExpression },
            { "GitIsDirty", BoolExpression },
        };

#pragma warning disable RS2008
        private static readonly DiagnosticDescriptor VIG0001 = new(
            id: "VIG0001",
            title: "VersionInfoGenerator was run without the necessary MSBuild properties",
            messageFormat: "VersionInfoGenerator was run without the necessary MSBuild properties",
            category: "",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/notpeelz/VersionInfoGenerator/wiki/Diagnostic-messages#vig0001");

        public void Execute(GeneratorExecutionContext context)
        {
            // XXX: we abort if this property is missing. This usually
            // happens when the consumer forgets to set PrivateAssets="all"
            // on the PackageReference.
            if (!TryGetMSBuildProperty("VersionInfoGenerateClass", out var generateStr))
            {
                context.ReportDiagnostic(Diagnostic.Create(VIG0001, null));
                return;
            }

            if (!bool.TryParse(generateStr, out var generate)) generate = true;
            if (!generate) return;

            var rootNamespace = GetMSBuildProperty("RootNamespace");
            var classNamespace = GetMSBuildProperty("VersionInfoClassNamespace");
            if (!bool.TryParse(GetMSBuildProperty("VersionInfoClassNamespaceGlobal"), out var useGlobalNamespace))
            {
                useGlobalNamespace = false;
            }

            if (useGlobalNamespace)
            {
                // VersionInfoClassNamespaceGlobal has precedence over
                // VersionInfoClassNamespace.
                classNamespace = null;
            }
            else
            {
                // If RootNamespace is null, it will fall back to the global
                // namespace.
                classNamespace = rootNamespace;
            }

            var className = GetMSBuildProperty("VersionInfoClassName") ?? "VersionInfo";
            var modifiers = GetMSBuildProperty("VersionInfoClassModifiers") ?? "internal static";

            var versionInfoClass = ClassDeclaration(className)
                .WithAttributeLists(List(new[]
                {
                    AttributeList(SingletonSeparatedList(
                        Attribute(IdentifierName("GeneratedCode"))
                            .WithArgumentList(AttributeArgumentList(SeparatedList<AttributeArgumentSyntax>(
                                new SyntaxNodeOrToken[]
                                {
                                    AttributeArgument(LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        Literal("VersionInfoGenerator"))),
                                    Token(SyntaxKind.CommaToken),
                                    AttributeArgument(LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        Literal(VersionInfoRuntime.SemVer)))
                                }))))),
                    AttributeList(SingletonSeparatedList(
                        Attribute(IdentifierName("CompilerGenerated")))),
                }))
                .WithModifiers(
                    TokenList(modifiers.Split(' ')
                        .Select(x => ModifierToken.GetToken(x))
                        .ToArray()))
                .WithMembers(
                    List(GenerateFields()
                        .Cast<MemberDeclarationSyntax>()
                        .ToArray()));

            var src = CompilationUnit()
                .WithUsings(List(new[]
                {
                    UsingDirective(QualifiedName(
                        QualifiedName(IdentifierName("System"), IdentifierName("CodeDom")),
                        IdentifierName("Compiler"))),
                    UsingDirective(QualifiedName(
                        QualifiedName(IdentifierName("System"), IdentifierName("Runtime")),
                        IdentifierName("CompilerServices")))
                }))
                .WithMembers(classNamespace != null
                    ? SingletonList<MemberDeclarationSyntax>(
                        NamespaceDeclaration(IdentifierName(classNamespace))
                            .WithMembers(SingletonList<MemberDeclarationSyntax>(versionInfoClass)))
                    // Use the global namespace if we don't have a namespace
                    : SingletonList<MemberDeclarationSyntax>(versionInfoClass))
                .NormalizeWhitespace();

            context.AddSource(
                hintName: classNamespace == null ? className : $"{classNamespace}.{className}",
                sourceText: SourceText.From(src.ToFullString(), Encoding.UTF8));

            IEnumerable<FieldDeclarationSyntax> GenerateFields()
            {
                var props = GetMSBuildProperty("VersionInfoClassSerializedProperties")
                    ?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim('\x20', '\r', '\n'))
                    .ToArray();
                foreach (var prop in SerializedProperties)
                {
                    if (props != null && props.Length > 0 && !props.Contains(prop.Key)) continue;

                    if (prop.Value is not LiteralExpressionProperty literalProp)
                    {
                        throw new InvalidOperationException($"Unsupported property value type: {prop.Value?.GetType()}");
                    }

                    var value = GetMSBuildProperty(prop.Key);
                    var (expr, type) = literalProp.GetExpression(value);

                    yield return FieldDeclaration(
                        VariableDeclaration(type)
                            .WithVariables(SingletonSeparatedList(
                                VariableDeclarator(Identifier(prop.Key))
                                    .WithInitializer(EqualsValueClause(expr)))))
                            .WithModifiers(TokenList(
                                new[]
                                {
                                    Token(SyntaxKind.PublicKeyword),
                                    Token(SyntaxKind.ConstKeyword)
                                }));
                }
            }

            string GetMSBuildProperty(string name)
            {
                if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{name}", out var value))
                {
                    throw new InvalidOperationException($"Missing MSBuild property: {name}");
                }

                if (string.IsNullOrEmpty(value)) return null;
                return value;
            }

            bool TryGetMSBuildProperty(string name, out string value)
            {
                if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{name}", out value))
                {
                    return false;
                }

                if (string.IsNullOrEmpty(value)) value = null;
                return true;
            }
        }
    }
}
