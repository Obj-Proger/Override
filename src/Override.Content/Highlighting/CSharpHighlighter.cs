using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Override.Content.Highlighting;

/// <summary>
/// Colors C# the way Visual Studio does: Roslyn classifies every token with full semantics,
/// so a class, a struct, a method and a local variable get different colors.
/// </summary>
internal static partial class CSharpHighlighter
{
    /// <summary>Snippets are written like in a modern project, where these namespaces are imported implicitly.</summary>
    private const string GlobalUsings = """
        global using System;
        global using System.Collections;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Threading;
        global using System.Threading.Tasks;
        """;

    private static readonly Lazy<Project> _project = new(CreateProject);

    public static string Highlight(string code)
    {
        var document = _project.Value.AddDocument("Snippet.cs", SourceText.From(code));
        var spans = Classifier
            .GetClassifiedSpansAsync(document, new TextSpan(0, code.Length))
            .GetAwaiter()
            .GetResult();

        var classes = new string?[code.Length];
        var priorities = new int[code.Length];

        foreach (var span in spans)
        {
            if (ClassificationTypeNames.AdditiveTypeNames.Contains(span.ClassificationType))
                continue;

            var (css, priority) = Classify(span.ClassificationType);
            if (css is null)
                continue;

            for (var i = span.TextSpan.Start; i < span.TextSpan.End && i < code.Length; i++)
            {
                if (priority >= priorities[i])
                {
                    classes[i] = css;
                    priorities[i] = priority;
                }
            }
        }

        MarkEscapes(document, classes, priorities);
        return TokenRuns.ToHtml(code, classes);
    }

    [GeneratedRegex("\\\\(?:u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8}|x[0-9A-Fa-f]{1,4}|.)")]
    private static partial Regex Escape();

    /// <summary>
    /// Escape sequences such as \n get their own color, as in Visual Studio. Roslyn does not mark them in every
    /// literal, so they are found in the tokens directly: characters, regular strings and regular interpolated strings.
    /// Verbatim and raw strings have no escapes.
    /// </summary>
    private static void MarkEscapes(Document document, string?[] classes, int[] priorities)
    {
        var root = document.GetSyntaxRootAsync().GetAwaiter().GetResult();
        if (root is null)
            return;

        foreach (var token in root.DescendantTokens())
        {
            if (!CanHaveEscapes(token))
                continue;

            foreach (Match match in Escape().Matches(token.Text))
            {
                for (var i = token.SpanStart + match.Index; i < token.SpanStart + match.Index + match.Length && i < classes.Length; i++)
                {
                    classes[i] = "str-esc";
                    priorities[i] = 3;
                }
            }
        }
    }

    private static bool CanHaveEscapes(SyntaxToken token) => token.Kind() switch
    {
        SyntaxKind.CharacterLiteralToken => true,
        SyntaxKind.StringLiteralToken or SyntaxKind.Utf8StringLiteralToken => token.Text.StartsWith('"'),
        SyntaxKind.InterpolatedStringTextToken =>
            token.Parent?.Parent is InterpolatedStringExpressionSyntax expression
            && expression.StringStartToken.IsKind(SyntaxKind.InterpolatedStringStartToken),
        _ => false,
    };

    /// <summary>
    /// CSS class for a Roslyn classification. A plain keyword has the lowest priority, so that a more
    /// specific classification of the same token, such as a control keyword, wins.
    /// </summary>
    private static (string? Css, int Priority) Classify(string type) => type switch
    {
        ClassificationTypeNames.Keyword => ("kw", 1),
        ClassificationTypeNames.ControlKeyword => ("kw-ctrl", 2),
        ClassificationTypeNames.ClassName
            or ClassificationTypeNames.RecordClassName
            or ClassificationTypeNames.DelegateName => ("type", 2),
        ClassificationTypeNames.StructName
            or ClassificationTypeNames.RecordStructName => ("type-struct", 2),
        ClassificationTypeNames.InterfaceName
            or ClassificationTypeNames.EnumName
            or ClassificationTypeNames.TypeParameterName => ("type-iface", 2),
        ClassificationTypeNames.StringLiteral
            or ClassificationTypeNames.VerbatimStringLiteral => ("str", 2),
        ClassificationTypeNames.StringEscapeCharacter => ("str-esc", 3),
        ClassificationTypeNames.NumericLiteral => ("num", 2),
        ClassificationTypeNames.Comment => ("cmt", 2),
        ClassificationTypeNames.MethodName
            or ClassificationTypeNames.ExtensionMethodName => ("method", 2),
        ClassificationTypeNames.LocalName
            or ClassificationTypeNames.ParameterName => ("local", 2),
        ClassificationTypeNames.PreprocessorKeyword
            or ClassificationTypeNames.PreprocessorText => ("pre", 2),
        _ when type.StartsWith("xml doc comment", StringComparison.Ordinal) => ("cmt", 2),
        _ => (null, 0),
    };

    /// <summary>A project with the framework of the running process, so that Console, List&lt;T&gt; and the rest resolve.</summary>
    private static Project CreateProject()
    {
        var frameworkDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => Path.GetDirectoryName(path) == frameworkDirectory)
            .Select(path => MetadataReference.CreateFromFile(path));

        var info = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            "Snippets",
            "Snippets",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.ConsoleApplication),
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
            metadataReferences: references);

        var workspace = new AdhocWorkspace();
        return workspace.AddProject(info).AddDocument("GlobalUsings.cs", GlobalUsings).Project;
    }
}
