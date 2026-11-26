using System.Text.Json.Serialization;
using Override.Learning;

namespace Override.Web.Progress;

/// <summary>What the user has seen of a topic: the furthest step, counted from 0, and the last visit.</summary>
public sealed record TopicVisit(int FurthestStep, DateTimeOffset LastVisit);

/// <summary>Everything stored about the user's learning. Kept in this browser only.</summary>
public sealed class ProgressData
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    /// <summary>Attempts by question key "topic-id/question-id", oldest first.</summary>
    public Dictionary<string, List<Attempt>> Questions { get; set; } = [];

    /// <summary>Visits by topic id.</summary>
    public Dictionary<string, TopicVisit> Topics { get; set; } = [];

    /// <summary>Topics passed by the quick check, with the time it was passed.</summary>
    public Dictionary<string, DateTimeOffset> KnownTopics { get; set; } = [];

    /// <summary>Results of topic tests by topic id, oldest first.</summary>
    public Dictionary<string, List<TestResult>> Tests { get; set; } = [];

    /// <summary>The version of each question shown last, by question key: 0 is the question, 1 and on its variants.</summary>
    public Dictionary<string, int> Variants { get; set; } = [];
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(ProgressData))]
[JsonSerializable(typeof(LearnerProfile))]
[JsonSerializable(typeof(TransferData))]
internal sealed partial class ProgressJsonContext : JsonSerializerContext
{
}

/// <summary>Everything a transfer link carries: the progress and the chosen level and direction.</summary>
public sealed record TransferData(int Version, ProgressData Progress, LearnerProfile? Profile);

/// <summary>How an answer ended and whether it counted as a first attempt.</summary>
public sealed record AnswerResult(Outcome Outcome, bool First, GivenAnswer? Given = null);

/// <summary>
/// What the user answered, to show it again after a test: picked options, typed text, the line pointed at or the
/// order of parsons lines (indices into the lines and distractors together).
/// </summary>
public sealed record GivenAnswer(
    IReadOnlyList<int>? Options = null, string? Text = null, int? Line = null, IReadOnlyList<int>? Order = null);

/// <summary>
/// The answers to one question: how many, how many were problems (wrong, forgotten, partly recalled), the last
/// outcome and where the question stands in repetition.
/// </summary>
public sealed record QuestionStat(string Id, int Answers, int Problems, Outcome Last, ReviewState Review);

/// <summary>A finished topic test: right answers out of all, the time it took and whether time ran out.</summary>
public sealed record TestResult(DateTimeOffset At, int Correct, int Total, int Seconds, bool TimedOut)
{
    public double Share => Total == 0 ? 0 : (double)Correct / Total;
}
