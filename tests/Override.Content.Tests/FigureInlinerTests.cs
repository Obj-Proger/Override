using Override.Content.Parsing;
using Override.Content.Rendering;

namespace Override.Content.Tests;

public sealed class FigureInlinerTests
{
    private const string Svg = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10" role="img" aria-labelledby="t d">
          <title id="t">Title</title>

          <desc id="d">Description</desc>
          <style>
            .big{fill:var(--fig-text,#26262C)}
            .a, .b{stroke:red}
          </style>
          <marker id="ah"><path d="M0 0L1 1"/></marker>
          <path class="big" d="M0 0L5 5" marker-end="url(#ah)"/>
          <use href="#ah"/>
        </svg>
        """;

    [Fact]
    public void InlinesFigureWithScopedIdsAndStyles()
    {
        using var content = new TempContent().Write("pic.svg", Svg);
        var diagnostics = new List<ContentDiagnostic>();

        var markdown = FigureInliner.Inline(
            "Before.\n![Alt text](pic.svg \"Caption\")\nAfter.", content.PathOf("topic.md"), 10, diagnostics);

        Assert.Empty(diagnostics);
        Assert.StartsWith("Before.\n<figure class=\"figure\" aria-label=\"Alt text\">\n<svg class=\"fig-pic\" xmlns=", markdown, StringComparison.Ordinal);
        Assert.EndsWith("<figcaption>Caption</figcaption>\n</figure>\nAfter.", markdown, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"fig-pic-t fig-pic-d\"", markdown, StringComparison.Ordinal);
        Assert.Contains("<title id=\"fig-pic-t\">", markdown, StringComparison.Ordinal);
        Assert.Contains("<marker id=\"fig-pic-ah\">", markdown, StringComparison.Ordinal);
        Assert.Contains("marker-end=\"url(#fig-pic-ah)\"", markdown, StringComparison.Ordinal);
        Assert.Contains("<use href=\"#fig-pic-ah\"/>", markdown, StringComparison.Ordinal);
        Assert.Contains(".fig-pic .big{fill:var(--fig-text,#26262C)}", markdown, StringComparison.Ordinal);
        Assert.Contains(".fig-pic .a,.fig-pic .b{stroke:red}", markdown, StringComparison.Ordinal);
        Assert.DoesNotContain("\n\n", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingFigureIsError()
    {
        using var content = new TempContent();
        var diagnostics = new List<ContentDiagnostic>();

        FigureInliner.Inline("Text.\n![Alt](missing.svg)", content.PathOf("topic.md"), 10, diagnostics);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(11, diagnostic.Line);
        Assert.Contains("'missing.svg' is not found", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RootWithClassIsError()
    {
        using var content = new TempContent().Write("pic.svg", Svg.Replace("<svg ", "<svg class=\"x\" ", StringComparison.Ordinal));
        var diagnostics = new List<ContentDiagnostic>();

        FigureInliner.Inline("![Alt](pic.svg)", content.PathOf("topic.md"), 1, diagnostics);

        Assert.Contains("must not have a class or an id", Assert.Single(diagnostics).Message, StringComparison.Ordinal);
    }
}
