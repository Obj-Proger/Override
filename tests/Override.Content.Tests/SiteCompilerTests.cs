using System.Text.Json;
using Override.Content.Model;
using Override.Content.Rendering;

namespace Override.Content.Tests;

public sealed class SiteCompilerTests
{
    private const string CodeTask = """
        ```quiz
        id: q-code
        type: code
        lang: csharp
        prompt: Return one.
        starter: |
          public static class One { public static int Get() => 0; }
        tests: |
          Assert.Equal(1, One.Get());
        solution: |
          public static class One { public static int Get() => RESULT; }
        explain: One.
        ```
        """;

    [Theory]
    [InlineData("1", true)]
    [InlineData("2", false)]
    public void ChecksReferenceSolutions(string result, bool passes)
    {
        var task = CodeTask.Replace("RESULT", result, StringComparison.Ordinal);
        using var content = new TempContent()
            .Section("lang/a", "Section A", 1)
            .Write("lang/a/sample.md", Samples.ValidTopic.Replace("::::practice", "::::practice\n\n" + task, StringComparison.Ordinal));

        var site = SiteCompiler.Compile(content.PathOf("lang"));

        if (passes)
            Assert.Empty(site.Diagnostics);
        else
            Assert.Contains(site.Diagnostics, d => d.Message.Contains("does not pass its tests", StringComparison.Ordinal));
    }

    [Fact]
    public void KeepsTheTestApartFromTheLesson()
    {
        using var content = new TempContent()
            .Section("lang/a", "Section A", 1)
            .Write("lang/a/sample.md", Samples.ValidTopic.Replace("order: 1", "order: 1\ntestMinutes: 5", StringComparison.Ordinal) + Samples.TestStep);

        var site = SiteCompiler.Compile(content.PathOf("lang"));
        Assert.Empty(site.Diagnostics);

        var page = Assert.Single(site.Pages);
        Assert.DoesNotContain(page.Steps, step => step.Kind == StepKind.Test);
        Assert.Equal(new[] { "t-one", "t-two" }, page.Test.Select(q => q.Quiz.Id));
        Assert.Equal(5, page.TestMinutes);

        var entry = Assert.Single(Assert.Single(site.Index.Sections).Topics);
        Assert.Equal(4, entry.Steps);
        Assert.Equal(2, entry.TestQuestions);
        Assert.DoesNotContain(entry.CheckedQuestions, q => q.Id.StartsWith("t-", StringComparison.Ordinal));
    }

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
        Assert.Empty(entry.Requires);

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
