namespace Override.Content.Model;

/// <summary>Metadata from the front matter of a topic file.</summary>
public sealed record TopicMetadata(
    string Id,
    string Title,
    Level Level,
    IReadOnlyList<Track> Tracks,
    int Minutes,
    int Order,
    IReadOnlyList<string> Requires,
    IReadOnlyList<string> QuickCheck,
    int? TestMinutes = null,
    int? TestSize = null);

/// <summary>A parsed topic: its metadata and the steps in file order, the test step last if there is one.</summary>
public sealed record Topic(TopicMetadata Metadata, IReadOnlyList<Step> Steps)
{
    /// <summary>How many questions one test attempt takes when the front matter does not say.</summary>
    public const int DefaultTestSize = 8;

    /// <summary>The questions of the test step: the pool each attempt draws from.</summary>
    public IReadOnlyList<QuizBlock> TestPool =>
        Steps.Where(step => step.Kind == StepKind.Test).SelectMany(step => step.Blocks).OfType<QuizBlock>().ToList();

    /// <summary>
    /// Questions in one attempt: as set in the front matter, otherwise 8, or fewer if the pool has fewer
    /// questions of the topic level. Questions of a higher level are shown only to readers of that level.
    /// </summary>
    public int TestSize => Metadata.TestSize
        ?? Math.Min(DefaultTestSize, TestPool.Count(block => block.Quiz.Level is null || block.Quiz.Level == Metadata.Level));
}

/// <summary>A lesson step. Line is where the step starts in the file.</summary>
public sealed record Step(StepKind Kind, string? Title, int Line, IReadOnlyList<Block> Blocks);

/// <summary>
/// A piece of a step. Level is set when the block sits inside a level container
/// and is then shown only from that level up.
/// </summary>
public abstract record Block(Level? Level, int Line);

/// <summary>Markdown text, rendered as is.</summary>
public sealed record MarkdownBlock(string Markdown, Level? Level, int Line) : Block(Level, Line);

/// <summary>
/// A question placed in the text, with other versions of it that the site shows in turn.
/// Part is set for questions of the test: the title of the lesson part the question checks.
/// </summary>
public sealed record QuizBlock(Quiz Quiz, Level? Level, int Line, IReadOnlyList<Quiz>? Variants = null, string? Part = null)
    : Block(Level, Line)
{
    /// <summary>The question and its variants.</summary>
    public IEnumerable<Quiz> AllVersions => Variants is null ? new[] { Quiz } : Variants.Prepend(Quiz);
}

/// <summary>A question read from YAML together with its variants.</summary>
public sealed record QuizWithVariants(Quiz Quiz, IReadOnlyList<Quiz> Variants, string? Part = null);
