using System.Text.Json;
using Override.Content.Model;
using Override.Content.Rendering;

namespace Override.Content.Tests;

public sealed class SiteCompilerTests
{
    [Fact]
    public void WritesIndexAndTopicsThatReadBack()
    {
        using var content = new TempContent()
            .Section("lang/a", "Section A", 1)
            .Write("lang/a/sample.md", Samples.ValidTopic);

        var site = SiteCompiler.Compile(content.PathOf("lang"));
        Assert.Empty(site.Diagnostics);

        var output = content.PathOf("out");
        SiteCompiler.Write(site, output);

        var index = JsonSerializer.Deserialize(
            File.ReadAllText(Path.Combine(output, SiteCompiler.IndexFileName)), ContentJsonContext.Default.SiteIndex)!;
        var section = Assert.Single(index.Sections);
        Assert.Equal("a", section.Path);
        Assert.Equal("Section A", section.Title);
        var entry = Assert.Single(section.Topics);
        Assert.Equal(("csharp.types.sample", "a/sample", 4), (entry.Id, entry.Path, entry.Steps));
        Assert.Equal(new[] { "q-output", "q-choice", "q-input" }, entry.CheckedQuestions.Select(q => q.Id));
        Assert.All(entry.CheckedQuestions, q => Assert.Equal(Level.Junior, q.Level));

        var page = JsonSerializer.Deserialize(
            File.ReadAllText(Path.Combine(output, SiteCompiler.TopicsDirectoryName, "csharp.types.sample.json")),
            ContentJsonContext.Default.TopicPage)!;
        Assert.Equal(
            new[] { StepKind.Warmup, StepKind.Part, StepKind.Practice, StepKind.Summary },
            page.Steps.Select(s => s.Kind));

        var part = page.Steps[1];
        Assert.Equal("First part", part.Title);
        Assert.Contains("<p>Some text.</p>", Assert.IsType<HtmlBlock>(part.Blocks[0]).Html, StringComparison.Ordinal);

        var choice = Assert.IsType<ChoiceQuiz>(part.Blocks.OfType<QuestionBlock>().Single().Quiz);
        Assert.Equal("Pick one.", choice.Prompt);
        Assert.Equal(new[] { "a", "b" }, choice.Options);
        Assert.Equal("<p>b is right.</p>", choice.Explain);

        var deeper = Assert.IsType<HtmlBlock>(part.Blocks[^1]);
        Assert.Equal(Level.Middle, deeper.Level);
        Assert.Equal("<p>Deeper text.</p>", deeper.Html);

        var recall = Assert.IsType<RecallQuiz>(Assert.IsType<QuestionBlock>(Assert.Single(page.Steps[^1].Blocks)).Quiz);
        Assert.Equal("<p>Reference answer.</p>", recall.Answer);
    }
}
