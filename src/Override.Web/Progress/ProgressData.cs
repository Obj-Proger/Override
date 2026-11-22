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
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(ProgressData))]
[JsonSerializable(typeof(LearnerProfile))]
internal sealed partial class ProgressJsonContext : JsonSerializerContext
{
}
