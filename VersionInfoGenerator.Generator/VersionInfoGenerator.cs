using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace VersionInfoGenerator.Generator
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

        public void Execute(GeneratorExecutionContext context)
        {
            if (!bool.TryParse(GetMSBuildProperty("VersionInfoGenerateClass"), out var shouldGenerate))
            {
                shouldGenerate = true;
            }
            if (!shouldGenerate) return;

            var rootNamespace = GetMSBuildProperty("RootNamespace");
            var classNamespace = GetMSBuildProperty("VersionInfoNamespace") ?? rootNamespace;
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
                    // Use the global namespace if we can't infer the namespace
                    : SingletonList<MemberDeclarationSyntax>(versionInfoClass))
                .NormalizeWhitespace();

            context.AddSource(
                hintName: rootNamespace == null ? className : $"{rootNamespace}.{className}",
                sourceText: SourceText.From(src.ToFullString(), Encoding.UTF8));

            IEnumerable<FieldDeclarationSyntax> GenerateFields()
            {
                foreach (var prop in SerializedProperties)
                {
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
        }
    }
}
