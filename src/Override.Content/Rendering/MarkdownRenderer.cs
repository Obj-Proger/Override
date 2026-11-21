using Markdig;

namespace Override.Content.Rendering;

/// <summary>Markdown to HTML with the extensions the content uses: tables and strikethrough.</summary>
internal static class MarkdownRenderer
{
    private const string ParagraphStart = "<p>";
    private const string ParagraphEnd = "</p>";

    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseEmphasisExtras()
        .Build();

    public static string Block(string markdown) => Markdown.ToHtml(markdown, _pipeline).Trim();

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
