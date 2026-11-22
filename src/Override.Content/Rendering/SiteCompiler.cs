using System.Text.Json;
using Override.Content.Checking;
using Override.Content.Model;
using Override.Content.Parsing;
using Override.Content.Site;

namespace Override.Content.Rendering;

/// <summary>The result of compiling one language: the table of contents and every topic page.</summary>
public sealed record CompiledSite(SiteIndex Index, IReadOnlyList<TopicPage> Pages, IReadOnlyList<ContentDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
}

/// <summary>Loads, checks and renders the content of one language, and writes it as JSON for the site.</summary>
public static class SiteCompiler
{
    public const string IndexFileName = "index.json";
    public const string TopicsDirectoryName = "topics";

    public static CompiledSite Compile(string languageDirectory)
    {
        var content = ContentLoader.Load(languageDirectory);
        var diagnostics = content.Diagnostics.ToList();
        SolutionVerifier.Verify(content.TopicsInOrder(), diagnostics);

        var pages = content.TopicsInOrder()
            .Select(topic => TopicRenderer.Render(topic, diagnostics))
            .ToList();

        var index = new SiteIndex(content.Sections.Select(ToEntry).ToList());
        return new CompiledSite(index, pages, diagnostics);
    }

    /// <summary>Replaces the output directory with index.json and topics/&lt;id&gt;.json.</summary>
    public static void Write(CompiledSite site, string outputDirectory)
    {
        if (Directory.Exists(outputDirectory))
            Directory.Delete(outputDirectory, recursive: true);

        var topicsDirectory = Path.Combine(outputDirectory, TopicsDirectoryName);
        Directory.CreateDirectory(topicsDirectory);

        File.WriteAllText(
            Path.Combine(outputDirectory, IndexFileName),
            JsonSerializer.Serialize(site.Index, ContentJsonContext.Default.SiteIndex));

        foreach (var page in site.Pages)
        {
            File.WriteAllText(
                Path.Combine(topicsDirectory, $"{page.Id}.json"),
                JsonSerializer.Serialize(page, ContentJsonContext.Default.TopicPage));
        }
    }

    private static SectionEntry ToEntry(SourceSection section) => new(
        section.Path,
        section.Title,
        section.Sections.Select(ToEntry).ToList(),
        section.Topics.Select(ToTopicEntry).ToList());

    private static TopicEntry ToTopicEntry(SourceTopic topic) => new(
        topic.Topic.Metadata.Id,
        topic.Path,
        topic.Topic.Metadata.Title,
        topic.Topic.Metadata.Level,
        topic.Topic.Metadata.Tracks,
        topic.Topic.Metadata.Minutes,
        topic.Topic.Steps.Count(step => step.Kind != StepKind.Test),
        topic.Topic.Steps
            .Where(step => step.Kind != StepKind.Test)
            .SelectMany(step => step.Blocks)
            .OfType<QuizBlock>()
            .Where(block => AnswerChecker.IsChecked(block.Quiz))
            .Select(block => new QuestionEntry(
                block.Quiz.Id,
                Highest(topic.Topic.Metadata.Level, block.Level, block.Quiz.Level),
                AnswerChecker.Runs(block.Quiz)))
            .ToList(),
        topic.Topic.Metadata.Requires,
        topic.Topic.Steps.Where(step => step.Kind == StepKind.Test).Sum(step => step.Blocks.Count));

    private static Level Highest(Level topic, Level? block, Level? question) =>
        (Level)Math.Max((int)topic, Math.Max((int)(block ?? topic), (int)(question ?? topic)));
}
