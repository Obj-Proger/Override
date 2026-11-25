using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Override.Content.Rendering;

/// <summary>Markdown to HTML with the extensions the content uses: tables, strikethrough and highlighted code.</summary>
internal static partial class MarkdownRenderer
{
    private const string ParagraphStart = "<p>";
    private const string ParagraphEnd = "</p>";

    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        // Only ~~strikethrough~~: superscript and subscript would turn 2^k and ~7 in plain text into markup.
        .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
        .Build();

    [GeneratedRegex("<table>.*?</table>", RegexOptions.Singleline)]
    private static partial Regex Table();

    public static string Block(string markdown)
    {
        var document = Markdown.Parse(markdown, _pipeline);

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        _pipeline.Setup(renderer);
        renderer.ObjectRenderers.ReplaceOrAdd<CodeBlockRenderer>(new HighlightingCodeBlockRenderer());
        renderer.Render(document);
        writer.Flush();

        return UnescapeTablePipes(writer.ToString().Trim());
    }

    /// <summary>
    /// A pipe inside a table cell is written as \| so that it does not split the cell. Markdig keeps the backslash
    /// inside code spans, so `a \|\| b` would show as is; in tables it is removed after rendering.
    /// </summary>
    private static string UnescapeTablePipes(string html) =>
        Table().Replace(html, table => table.Value.Replace("\\|", "|", StringComparison.Ordinal));

    /// <summary>
    /// Renders a short text such as a question prompt or an option. A single paragraph loses its
    /// &lt;p&gt; wrapper so that the text can sit inside a button or a label.
    /// </summary>
    public static string Inline(string markdown)
    {
        var html = Block(markdown);
        var isSingleParagraph = html.StartsWith(ParagraphStart, StringComparison.Ordinal)
            && html.EndsWith(ParagraphEnd, StringComparison.Ordinal)
            && html.IndexOf(ParagraphStart, ParagraphStart.Length, StringComparison.Ordinal) < 0;

        return isSingleParagraph ? html[ParagraphStart.Length..^ParagraphEnd.Length] : html;
    }
}
