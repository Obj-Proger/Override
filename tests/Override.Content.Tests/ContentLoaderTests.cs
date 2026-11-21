using Override.Content.Parsing;
using Override.Content.Site;

namespace Override.Content.Tests;

public sealed class ContentLoaderTests
{
    [Fact]
    public void LoadsTopicsInProgramOrder()
    {
        using var content = new TempContent()
            .Section("b", "B", 2)
            .Write("b/w.md", Samples.Topic("b.w"))
            .Section("a", "A", 1)
            .Write("a/x.md", Samples.Topic("a.x", order: 2))
            .Write("a/y.md", Samples.Topic("a.y", order: 1))
            .Section("a/sub", "Sub", 1)
            .Write("a/sub/z.md", Samples.Topic("a.z"));

        var result = ContentLoader.Load(content.Root);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(new[] { "A", "B" }, result.Sections.Select(s => s.Title));
        Assert.Equal(new[] { "a/y", "a/x", "a/sub/z", "b/w" }, result.TopicsInOrder().Select(t => t.Path));
    }

    [Fact]
    public void SectionWithoutSectionFileIsError()
    {
        using var content = new TempContent().Write("a/x.md", Samples.Topic("a.x"));

        AssertSingle(ContentLoader.Load(content.Root), DiagnosticSeverity.Error, "needs a _section.yml");
    }

    [Fact]
    public void FolderNameMustBeSlug()
    {
        using var content = new TempContent().Section("Bad_Name", "Bad", 1);

        AssertSingle(ContentLoader.Load(content.Root), DiagnosticSeverity.Error, "must be lower-case words");
    }

    [Fact]
    public void TopicInLanguageFolderIsError()
    {
        using var content = new TempContent().Write("x.md", Samples.Topic("a.x"));

        AssertSingle(ContentLoader.Load(content.Root), DiagnosticSeverity.Error, "inside a section folder");
    }

    [Fact]
    public void RequiredTopicMustComeEarlier()
    {
        using var content = new TempContent()
            .Section("a", "A", 1)
            .Write("a/one.md", Samples.Topic("a.one", 1, "a.two"))
            .Write("a/two.md", Samples.Topic("a.two", 2));

        AssertSingle(ContentLoader.Load(content.Root), DiagnosticSeverity.Error, "must come earlier");
    }

    [Fact]
    public void RequiredTopicNotWrittenYetIsWarning()
    {
        using var content = new TempContent()
            .Section("a", "A", 1)
            .Write("a/one.md", Samples.Topic("a.one", 1, "a.missing"));

        var result = ContentLoader.Load(content.Root);

        Assert.False(result.HasErrors);
        AssertSingle(result, DiagnosticSeverity.Warning, "'a.missing' is not written yet");
    }

    [Fact]
    public void TopicIdsAreUnique()
    {
        using var content = new TempContent()
            .Section("a", "A", 1)
            .Write("a/one.md", Samples.Topic("a.same", 1))
            .Write("a/two.md", Samples.Topic("a.same", 2));

        AssertSingle(ContentLoader.Load(content.Root), DiagnosticSeverity.Error, "is used by another topic");
    }

    private static void AssertSingle(ContentSet result, DiagnosticSeverity severity, string expected)
    {
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(severity, diagnostic.Severity);
        Assert.Contains(expected, diagnostic.Message, StringComparison.Ordinal);
    }
}
