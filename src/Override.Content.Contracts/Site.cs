using System.Text.Json.Serialization;

namespace Override.Content.Model;

/// <summary>The table of contents of the site: the section tree with short topic entries.</summary>
public sealed record SiteIndex(IReadOnlyList<SectionEntry> Sections);

/// <summary>A section. Path is its address on the site, e.g. dotnet/csharp/type-system.</summary>
public sealed record SectionEntry(
    string Path,
    string Title,
    IReadOnlyList<SectionEntry> Sections,
    IReadOnlyList<TopicEntry> Topics);

/// <summary>
/// A topic in the table of contents. Path is its address on the site. Steps is the number of lesson steps;
/// CheckedQuestions are the ids of the questions the site checks by itself, which decide whether the topic is done.
/// </summary>
public sealed record TopicEntry(
    string Id,
    string Path,
    string Title,
    Level Level,
    IReadOnlyList<Track> Tracks,
    int Minutes,
    int Steps,
    IReadOnlyList<string> CheckedQuestions);

/// <summary>A compiled topic, ready to show.</summary>
public sealed record TopicPage(
    string Id,
    string Path,
    string Title,
    Level Level,
    IReadOnlyList<Track> Tracks,
    int Minutes,
    IReadOnlyList<string> Requires,
    IReadOnlyList<string> QuickCheck,
    IReadOnlyList<PageStep> Steps);

/// <summary>A lesson step of a compiled topic.</summary>
public sealed record PageStep(StepKind Kind, string? Title, IReadOnlyList<PageBlock> Blocks);

/// <summary>A piece of a step. Level is set when it is shown only from that level up.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(HtmlBlock), "html")]
[JsonDerivedType(typeof(QuestionBlock), "question")]
public abstract record PageBlock(Level? Level);

/// <summary>Rendered text.</summary>
public sealed record HtmlBlock(Level? Level, string Html) : PageBlock(Level);

/// <summary>
/// A question in the text. CodeHtml is the highlighted Quiz.Code, which stays plain for checking answers.
/// LinesHtml holds highlighted lines for questions that work with single lines: the code lines of find-bug,
/// and for parsons the lines followed by the distractors.
/// </summary>
public sealed record QuestionBlock(Level? Level, Quiz Quiz, string? CodeHtml, IReadOnlyList<string>? LinesHtml)
    : PageBlock(Level);
