using System.Text.Json;
using Microsoft.JSInterop;
using Override.Content.Model;
using Override.Learning;

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
/// Records answers and visits and keeps them in local storage. Components subscribe to Changed to redraw;
/// it also fires when the reader's level changes, since the status of topics depends on it.
/// </summary>
public sealed class ProgressStore
{
    private const string StorageKey = "override.progress";

    /// <summary>Older attempts are dropped: the recent ones tell what the user knows now.</summary>
    private const int MaxAttemptsPerQuestion = 20;

    /// <summary>An answer within this time after the previous one is a retry, not a fresh attempt.</summary>
    private static readonly TimeSpan _retryWindow = TimeSpan.FromHours(8);

    private readonly IJSRuntime _js;
    private readonly ProfileStore _profile;
    private ProgressData _data = new();

    public ProgressStore(IJSRuntime js, ProfileStore profile)
    {
        _js = js;
        _profile = profile;
        _profile.Changed += () => Changed?.Invoke();
    }

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

    /// <summary>The checked questions of the topic at the reader's level and below: only they decide whether it is done.</summary>
    public IReadOnlyList<QuestionEntry> QuestionsFor(TopicEntry topic) =>
        topic.CheckedQuestions.Where(q => _profile.Shows(q.Level)).ToList();

    public int KnownCount(TopicEntry topic) => QuestionsFor(topic).Count(q => IsKnown(topic.Id, q.Id));

    public TopicStatus StatusOf(TopicEntry topic)
    {
        var questions = QuestionsFor(topic);
        var visited = _data.Topics.TryGetValue(topic.Id, out var visit);

        // A topic without checked questions at this level is done once its last step was opened.
        var done = questions.Count > 0
            ? questions.All(q => IsKnown(topic.Id, q.Id))
            : visited && visit!.FurthestStep >= topic.Steps - 1;

        if (done)
            return TopicStatus.Done;

        var answeredAny = topic.CheckedQuestions.Any(q => AttemptsOf(topic.Id, q.Id).Count > 0);
        return visited || answeredAny ? TopicStatus.Started : TopicStatus.NotStarted;
    }

    public ReviewState ReviewOf(string topicId, string questionId) => ReviewSchedule.StateOf(AttemptsOf(topicId, questionId));

    /// <summary>How well the topic is known now, from 0 to 1, by the checked questions at the reader's level.</summary>
    public double StrengthOf(TopicEntry topic) =>
        Strength.OfTopic(QuestionsFor(topic).Select(q => ReviewOf(topic.Id, q.Id)), DateTimeOffset.Now);

    /// <summary>
    /// Review states of every answered question of the topic, recall questions included, since their gaps
    /// are planned for review too. Questions above the reader's level are left out.
    /// </summary>
    public IReadOnlyList<ReviewState> ReviewsOf(TopicEntry topic)
    {
        var hidden = topic.CheckedQuestions.Where(q => !_profile.Shows(q.Level)).Select(q => q.Id).ToHashSet();
        var prefix = topic.Id + "/";

        return _data.Questions
            .Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal) && !hidden.Contains(pair.Key[prefix.Length..]))
            .Select(pair => ReviewSchedule.StateOf(pair.Value))
            .ToList();
    }

    /// <summary>Questions of the topic that are due for review now.</summary>
    public int DueCount(TopicEntry topic)
    {
        var now = DateTimeOffset.Now;
        return ReviewsOf(topic).Count(state => state.IsDue(now));
    }

    /// <summary>The nearest planned review of the topic, or null when nothing is planned.</summary>
    public DateTimeOffset? NextReview(TopicEntry topic) => ReviewsOf(topic).Min(state => state.Due);

    /// <summary>The last counted answer to some question of the topic was a problem.</summary>
    public bool HasGaps(TopicEntry topic) => ReviewsOf(topic).Any(state => state.LastWasProblem);

    private IJSInProcessRuntime Storage => (IJSInProcessRuntime)_js;

    private static string Key(string topicId, string questionId) => $"{topicId}/{questionId}";

    private void Save()
    {
        Storage.Invoke<bool>("overrideStorage.set", StorageKey, JsonSerializer.Serialize(_data, ProgressJsonContext.Default.ProgressData));
        Changed?.Invoke();
    }
}
