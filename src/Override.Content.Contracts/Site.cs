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
/// CheckedQuestions are the questions the site checks by itself, which decide whether the topic is done.
/// Requires are the ids of the topics to know first. TestQuestions is the number of questions in one test attempt,
/// 0 without a test. QuestionIds lists every question of the topic, test included: answers to questions that are
/// gone from the content are left out of reviews.
/// </summary>
public sealed record TopicEntry(
    string Id,
    string Path,
    string Title,
    Level Level,
    IReadOnlyList<Track> Tracks,
    int Minutes,
    int Steps,
    IReadOnlyList<QuestionEntry> CheckedQuestions,
    IReadOnlyList<string> Requires,
    int TestQuestions,
    IReadOnlyList<string>? QuestionIds = null);

/// <summary>
/// A question of a topic with the level it is meant for: its own, its block's or the topic's, whichever is higher.
/// Runs is true for tasks checked by running code, which need the compiler to be loaded.
/// </summary>
public sealed record QuestionEntry(string Id, Level Level, bool Runs = false);

/// <summary>
/// A compiled topic, ready to show. Steps are the lesson; Test is the pool of test questions, empty when there is
/// no test. Each attempt takes TestSize of them, at least one per lesson part when it can. TestMinutes is the
/// time limit, null for a test without a timer.
/// </summary>
public sealed record TopicPage(
    string Id,
    string Path,
    string Title,
    Level Level,
    IReadOnlyList<Track> Tracks,
    int Minutes,
    IReadOnlyList<string> Requires,
    IReadOnlyList<string> QuickCheck,
    IReadOnlyList<PageStep> Steps,
    IReadOnlyList<QuestionBlock> Test,
    int? TestMinutes,
    int TestSize = 0);

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
/// and for parsons the lines followed by the distractors. SolutionHtml is the highlighted reference solution
/// of a code or SQL task. Variants are other versions of the same question, with the same id: the site
/// shows them in turn so that answers are not learned by heart. Part is set for test questions: the title of the
/// lesson part the question checks.
/// </summary>
public sealed record QuestionBlock(
    Level? Level,
    Quiz Quiz,
    string? CodeHtml,
    IReadOnlyList<string>? LinesHtml,
    string? SolutionHtml = null,
    IReadOnlyList<QuestionBlock>? Variants = null,
    string? Part = null)
    : PageBlock(Level);
