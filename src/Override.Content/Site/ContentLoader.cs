using System.Text.RegularExpressions;
using Override.Content.Parsing;

namespace Override.Content.Site;

/// <summary>
/// Loads the content of one language: every folder below it is a section described by _section.yml,
/// every .md file in a section is a topic. Then checks the rules that span topics.
/// </summary>
public static partial class ContentLoader
{
    public const string SectionFileName = "_section.yml";

    /// <summary>Folder and file names become addresses on the site, so they are limited to lower-case words.</summary>
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex Slug();

    public static ContentSet Load(string languageDirectory)
    {
        var root = Path.GetFullPath(languageDirectory);
        var diagnostics = new List<ContentDiagnostic>();

        foreach (var file in Directory.EnumerateFiles(root, "*.md"))
            diagnostics.Add(new(file, 1, "Topics must be inside a section folder, not in the language folder."));

        var sections = Directory.EnumerateDirectories(root)
            .Order(StringComparer.Ordinal)
            .Select(directory => LoadSection(directory, "", diagnostics))
            .OfType<SourceSection>()
            .OrderBy(s => s.Order)
            .ThenBy(s => s.Title, StringComparer.Ordinal)
            .ToList();

        var content = new ContentSet(sections, diagnostics);
        CheckTopics(content, diagnostics);
        return content;
    }

    private static SourceSection? LoadSection(string directory, string parentPath, List<ContentDiagnostic> diagnostics)
    {
        var name = Path.GetFileName(directory);
        var path = parentPath.Length == 0 ? name : $"{parentPath}/{name}";
        var sectionFile = Path.Combine(directory, SectionFileName);

        if (!Slug().IsMatch(name))
        {
            diagnostics.Add(new(directory, 1, $"Folder name '{name}' must be lower-case words joined by hyphens: it is part of the address."));
            return null;
        }

        if (!File.Exists(sectionFile))
        {
            diagnostics.Add(new(sectionFile, 1, $"Section folder '{path}' needs a {SectionFileName} file with its title and order."));
            return null;
        }

        var errorsBefore = ErrorCount(diagnostics);
        var header = Yaml.Read<SectionDocument>(File.ReadAllText(sectionFile), sectionFile, 1, diagnostics);
        if (header is not null)
        {
            if (string.IsNullOrWhiteSpace(header.Title))
                diagnostics.Add(new(sectionFile, 1, "'title' is required."));

            if (header.Order is not >= 0)
                diagnostics.Add(new(sectionFile, 1, "'order' is required: 0 or more."));
        }

        var topics = new List<SourceTopic>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.md").Order(StringComparer.Ordinal))
        {
            var slug = Path.GetFileNameWithoutExtension(file);
            if (!Slug().IsMatch(slug))
            {
                diagnostics.Add(new(file, 1, $"File name '{slug}' must be lower-case words joined by hyphens: it is part of the address."));
                continue;
            }

            var result = TopicParser.Parse(file, File.ReadAllText(file));
            diagnostics.AddRange(result.Diagnostics);
            if (result.Topic is not null)
                topics.Add(new SourceTopic($"{path}/{slug}", file, result.Topic));
        }

        var sections = Directory.EnumerateDirectories(directory)
            .Order(StringComparer.Ordinal)
            .Select(child => LoadSection(child, path, diagnostics))
            .OfType<SourceSection>()
            .OrderBy(s => s.Order)
            .ThenBy(s => s.Title, StringComparer.Ordinal)
            .ToList();

        // A broken section is left out: the build fails anyway, and the cross-topic checks see only sound topics.
        if (ErrorCount(diagnostics) != errorsBefore || header is null)
            return null;

        var orderedTopics = topics
            .OrderBy(t => t.Topic.Metadata.Order)
            .ThenBy(t => t.Topic.Metadata.Title, StringComparer.Ordinal)
            .ToList();

        return new SourceSection(path, header.Title!.Trim(), header.Order!.Value, sections, orderedTopics);
    }

    private static int ErrorCount(IEnumerable<ContentDiagnostic> diagnostics) =>
        diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// Topic ids are unique, and a required topic comes earlier in the program, which also rules out cycles.
    /// A required topic that is not written yet is a warning: content is written gradually.
    /// </summary>
    private static void CheckTopics(ContentSet content, List<ContentDiagnostic> diagnostics)
    {
        var topics = content.TopicsInOrder().ToList();
        var allIds = topics.Select(t => t.Topic.Metadata.Id).ToHashSet(StringComparer.Ordinal);
        var earlierIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var topic in topics)
        {
            var metadata = topic.Topic.Metadata;
            foreach (var required in metadata.Requires)
            {
                if (earlierIds.Contains(required))
                    continue;

                if (allIds.Contains(required))
                {
                    diagnostics.Add(new(topic.FilePath, 1, $"Required topic '{required}' must come earlier in the program than '{metadata.Id}'."));
                }
                else
                {
                    diagnostics.Add(new(
                        topic.FilePath, 1, $"Required topic '{required}' is not written yet.", DiagnosticSeverity.Warning));
                }
            }

            if (!earlierIds.Add(metadata.Id))
                diagnostics.Add(new(topic.FilePath, 1, $"Topic id '{metadata.Id}' is used by another topic."));
        }
    }

    /// <summary>A _section.yml file as written in YAML.</summary>
    internal sealed class SectionDocument
    {
        public string? Title { get; set; }

        public int? Order { get; set; }
    }
}
