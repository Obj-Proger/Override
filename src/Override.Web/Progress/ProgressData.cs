using System.Text.Json.Serialization;

namespace Override.Web.Progress;

/// <summary>How an attempt at a question ended.</summary>
public enum Outcome
{
    Correct,
    Wrong,

    /// <summary>Self-assessment after seeing the reference: did not recall. Counts as a problem.</summary>
    Forgot,

    /// <summary>Self-assessment: recalled part of it. Counts as a problem.</summary>
    Partial,

    /// <summary>Self-assessment: recalled. Kept for history, but never counts as knowledge.</summary>
    Recalled,
}

/// <summary>
/// One answer. First is true when the question had no answer in the hours before: only such attempts
/// show what the user knows, retries after reading the explanation do not.
/// </summary>
public sealed record Attempt(DateTimeOffset At, Outcome Outcome, bool First);

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
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(ProgressData))]
[JsonSerializable(typeof(LearnerProfile))]
internal sealed partial class ProgressJsonContext : JsonSerializerContext
{
}
