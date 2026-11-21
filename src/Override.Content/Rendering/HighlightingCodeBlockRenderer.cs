using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Override.Content.Highlighting;

namespace Override.Content.Rendering;

/// <summary>Renders Markdown code blocks with build-time highlighting instead of plain &lt;pre&gt;&lt;code&gt;.</summary>
internal sealed class HighlightingCodeBlockRenderer : HtmlObjectRenderer<CodeBlock>
{
    protected override void Write(HtmlRenderer renderer, CodeBlock obj)
    {
        var language = obj is FencedCodeBlock fenced ? fenced.Info : null;

        renderer.EnsureLine();
        renderer.Write(CodeHighlighter.Block(obj.Lines.ToString(), language));
        renderer.WriteLine();
    }
}
