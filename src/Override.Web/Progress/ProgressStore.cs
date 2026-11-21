using System.Text.Json;
using Microsoft.JSInterop;
using Override.Content.Model;

namespace Override.Web.Progress;

/// <summary>Status of a topic in the program.</summary>
public enum TopicStatus
{
    NotStarted,
    Started,

    /// <summary>Every question the site checks was answered right at the first attempt at least once.</summary>
    Done,
}

/// <summary>
/// Records answers and visits and keeps them in local storage. Components subscribe to Changed to redraw.
/// </summary>
public sealed class ProgressStore(IJSRuntime js)
{
    private const string StorageKey = "override.progress";

    /// <summary>Older attempts are dropped: the recent ones tell what the user knows now.</summary>
    private const int MaxAttemptsPerQuestion = 20;

    /// <summary>An answer within this time after the previous one is a retry, not a fresh attempt.</summary>
    private static readonly TimeSpan _retryWindow = TimeSpan.FromHours(8);

    private ProgressData _data = new();

    public event Action? Changed;

    /// <summary>Reads saved progress. Unreadable data is replaced with empty progress rather than breaking the site.</summary>
    public void Load()
    {
        var json = Storage.Invoke<string?>("overrideStorage.get", StorageKey);
        if (string.IsNullOrEmpty(json))
            return;

        try
        {
            var data = JsonSerializer.Deserialize(json, ProgressJsonContext.Default.ProgressData);
            if (data is { Version: ProgressData.CurrentVersion })
                _data = data;
        }
        catch (JsonException)
        {
            // Damaged data: start over. The site must open even if storage was edited by hand.
        }
    }

    public IReadOnlyList<Attempt> AttemptsOf(string topicId, string questionId) =>
        _data.Questions.TryGetValue(Key(topicId, questionId), out var attempts) ? attempts : [];

    /// <summary>Saves an answer and returns whether it counted as a first attempt.</summary>
    public bool Record(string topicId, string questionId, Outcome outcome)
    {
        var now = DateTimeOffset.Now;
        var key = Key(topicId, questionId);
        if (!_data.Questions.TryGetValue(key, out var attempts))
            _data.Questions[key] = attempts = [];

        var first = attempts.Count == 0 || now - attempts[^1].At > _retryWindow;
        attempts.Add(new Attempt(now, outcome, first));
        if (attempts.Count > MaxAttemptsPerQuestion)
            attempts.RemoveRange(0, attempts.Count - MaxAttemptsPerQuestion);

        Save();
        return first;
    }

    public void Visit(string topicId, int step)
    {
        var furthest = _data.Topics.TryGetValue(topicId, out var visit) ? Math.Max(visit.FurthestStep, step) : step;
        _data.Topics[topicId] = new TopicVisit(furthest, DateTimeOffset.Now);
        Save();
    }

    /// <summary>Whether this question was ever answered right at the first attempt.</summary>
    public bool IsKnown(string topicId, string questionId) =>
        AttemptsOf(topicId, questionId).Any(a => a.First && a.Outcome == Outcome.Correct);

    public int KnownCount(TopicEntry topic) => topic.CheckedQuestions.Count(id => IsKnown(topic.Id, id));

    public TopicStatus StatusOf(TopicEntry topic)
    {
        var visited = _data.Topics.TryGetValue(topic.Id, out var visit);
        var known = KnownCount(topic);

        // A topic without checked questions is done once its last step was opened.
        var done = topic.CheckedQuestions.Count > 0
            ? known == topic.CheckedQuestions.Count
            : visited && visit!.FurthestStep >= topic.Steps - 1;

        if (done)
            return TopicStatus.Done;

        var answeredAny = topic.CheckedQuestions.Any(id => AttemptsOf(topic.Id, id).Count > 0);
        return visited || answeredAny ? TopicStatus.Started : TopicStatus.NotStarted;
    }

    private IJSInProcessRuntime Storage => (IJSInProcessRuntime)js;

    private static string Key(string topicId, string questionId) => $"{topicId}/{questionId}";

    private void Save()
    {
        Storage.Invoke<bool>("overrideStorage.set", StorageKey, JsonSerializer.Serialize(_data, ProgressJsonContext.Default.ProgressData));
        Changed?.Invoke();
    }
}
