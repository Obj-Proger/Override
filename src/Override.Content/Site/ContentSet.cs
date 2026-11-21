using Override.Content.Model;
using Override.Content.Parsing;

namespace Override.Content.Site;

/// <summary>A section folder with its _section.yml. Path is relative to the language folder, with / separators.</summary>
public sealed record SourceSection(
    string Path,
    string Title,
    int Order,
    IReadOnlyList<SourceSection> Sections,
    IReadOnlyList<SourceTopic> Topics);

/// <summary>A parsed topic file. Path is its site address; FilePath is the full path on disk.</summary>
public sealed record SourceTopic(string Path, string FilePath, Topic Topic);

/// <summary>All sections and topics of one language, in program order, with every problem found.</summary>
public sealed record ContentSet(IReadOnlyList<SourceSection> Sections, IReadOnlyList<ContentDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>Topics in the order of the program: sections depth first, then topics within a section.</summary>
    public IEnumerable<SourceTopic> TopicsInOrder() => Sections.SelectMany(Walk);

    private static IEnumerable<SourceTopic> Walk(SourceSection section) =>
        section.Topics.Concat(section.Sections.SelectMany(Walk));
}
