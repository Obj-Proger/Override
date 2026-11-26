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

    /// <summary>The share of right answers that passes a topic test.</summary>
    public const double TestPassShare = 0.75;

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

    /// <summary>The stored data as is, for a transfer link.</summary>
    internal ProgressData Data => _data;

    /// <summary>
    /// Takes progress from another device. Replace drops what is here; merge keeps both: answers and test results
    /// are joined by time, the furthest step and the latest visit win.
    /// </summary>
    public void Import(ProgressData other, bool replace)
    {
        if (replace)
        {
            _data = other;
            Save();
            return;
        }

        foreach (var (key, attempts) in other.Questions)
        {
            var joined = (_data.Questions.TryGetValue(key, out var mine) ? mine.Concat(attempts) : attempts)
                .DistinctBy(attempt => (attempt.At, attempt.Outcome))
                .OrderBy(attempt => attempt.At)
                .ToList();
            if (joined.Count > MaxAttemptsPerQuestion)
                joined.RemoveRange(0, joined.Count - MaxAttemptsPerQuestion);
            _data.Questions[key] = joined;
        }

        foreach (var (id, visit) in other.Topics)
        {
            _data.Topics[id] = _data.Topics.TryGetValue(id, out var mine)
                ? new TopicVisit(Math.Max(mine.FurthestStep, visit.FurthestStep), mine.LastVisit > visit.LastVisit ? mine.LastVisit : visit.LastVisit)
                : visit;
        }

        foreach (var (id, at) in other.KnownTopics)
        {
            if (!_data.KnownTopics.TryGetValue(id, out var mine) || at < mine)
                _data.KnownTopics[id] = at;
        }

        foreach (var (id, results) in other.Tests)
        {
            _data.Tests[id] = (_data.Tests.TryGetValue(id, out var mine) ? mine.Concat(results) : results)
                .DistinctBy(result => result.At)
                .OrderBy(result => result.At)
                .ToList();
        }

        foreach (var (key, index) in other.Variants)
            _data.Variants.TryAdd(key, index);

        Save();
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

    /// <summary>Changes the outcome of the last answer, keeping its time and whether it was first.</summary>
    public void Amend(string topicId, string questionId, Outcome outcome)
    {
        if (!_data.Questions.TryGetValue(Key(topicId, questionId), out var attempts) || attempts.Count == 0)
            return;

        attempts[^1] = attempts[^1] with { Outcome = outcome };
        Save();
    }

    /// <summary>
    /// Chooses the version of a question to show. The first time it is the question itself, as the author wrote it;
    /// later a random version other than the one shown last, so that the answer cannot be learned by heart.
    /// </summary>
    public QuestionBlock PickVariant(string topicId, QuestionBlock question)
    {
        if (question.Variants is not { Count: > 0 } variants)
            return question;

        var key = Key(topicId, question.Quiz.Id);
        var count = variants.Count + 1;
        var index = 0;
        if (_data.Variants.TryGetValue(key, out var last) && last >= 0 && last < count)
        {
            index = Random.Shared.Next(count - 1);
            if (index >= last)
                index++;
        }

        _data.Variants[key] = index;
        SaveQuietly();
        return index == 0 ? question : variants[index - 1];
    }

    public void Visit(string topicId, int step)
    {
        var furthest = _data.Topics.TryGetValue(topicId, out var visit) ? Math.Max(visit.FurthestStep, step) : step;
        _data.Topics[topicId] = new TopicVisit(furthest, DateTimeOffset.Now);
        Save();
    }

    /// <summary>Marks a topic as known after a passed quick check.</summary>
    public void MarkKnown(string topicId)
    {
        _data.KnownTopics[topicId] = DateTimeOffset.Now;
        Save();
    }

    public bool PassedQuickCheck(string topicId) => _data.KnownTopics.ContainsKey(topicId);

    public void RecordTest(string topicId, TestResult result)
    {
        if (!_data.Tests.TryGetValue(topicId, out var results))
            _data.Tests[topicId] = results = [];

        results.Add(result);
        if (results.Count > MaxAttemptsPerQuestion)
            results.RemoveRange(0, results.Count - MaxAttemptsPerQuestion);

        Save();
    }

    /// <summary>The best result of the topic test, or null if it was never taken.</summary>
    public TestResult? BestTest(string topicId) =>
        _data.Tests.TryGetValue(topicId, out var results)
            ? results.OrderByDescending(r => r.Share).ThenByDescending(r => r.At).FirstOrDefault()
            : null;

    /// <summary>Whether the topic test was ever passed.</summary>
    public bool PassedTest(string topicId) => BestTest(topicId) is { } best && best.Share >= TestPassShare;

    /// <summary>Whether this question was ever answered right at the first attempt.</summary>
    public bool IsKnown(string topicId, string questionId) =>
        AttemptsOf(topicId, questionId).Any(a => a.First && a.Outcome == Outcome.Correct);

    /// <summary>The checked questions of the topic at the reader's level and below: only they decide whether it is done.</summary>
    public IReadOnlyList<QuestionEntry> QuestionsFor(TopicEntry topic) =>
        topic.CheckedQuestions.Where(q => _profile.Shows(q.Level, topic.Path)).ToList();

    public int KnownCount(TopicEntry topic) => QuestionsFor(topic).Count(q => IsKnown(topic.Id, q.Id));

    public TopicStatus StatusOf(TopicEntry topic)
    {
        var questions = QuestionsFor(topic);
        var visited = _data.Topics.TryGetValue(topic.Id, out var visit);

        // A passed test proves the topic: it has new questions, answered without hints.
        // A passed quick check counts until a later answer shows a gap.
        // A topic without checked questions at this level is done once its last step was opened.
        var done = PassedTest(topic.Id)
            || (PassedQuickCheck(topic.Id) && !HasGaps(topic))
            || (questions.Count > 0
                ? questions.All(q => IsKnown(topic.Id, q.Id))
                : visited && visit!.FurthestStep >= topic.Steps - 1);

        if (done)
            return TopicStatus.Done;

        var answeredAny = topic.CheckedQuestions.Any(q => AttemptsOf(topic.Id, q.Id).Count > 0);
        return visited || answeredAny ? TopicStatus.Started : TopicStatus.NotStarted;
    }

    public ReviewState ReviewOf(string topicId, string questionId) => ReviewSchedule.StateOf(AttemptsOf(topicId, questionId));

    /// <summary>
    /// How well the topic is known now, from 0 to 1, by the checked questions at the reader's level.
    /// After a passed quick check only the answered questions count: the rest were skipped on purpose.
    /// </summary>
    public double StrengthOf(TopicEntry topic)
    {
        var states = QuestionsFor(topic).Select(q => ReviewOf(topic.Id, q.Id)).ToList();
        if (PassedQuickCheck(topic.Id) && states.Any(s => s.Box > 0))
            states = states.Where(s => s.Box > 0).ToList();

        return Strength.OfTopic(states, DateTimeOffset.Now);
    }

    /// <summary>
    /// Review states of every answered question of the topic, recall questions included, since their gaps
    /// are planned for review too. Questions above the reader's level are left out.
    /// </summary>
    public IReadOnlyList<ReviewState> ReviewsOf(TopicEntry topic) => ReviewEntries(topic).Select(e => e.State).ToList();

    /// <summary>Ids of the topic's questions that are due for review now.</summary>
    public IReadOnlyList<string> DueQuestions(TopicEntry topic)
    {
        var now = DateTimeOffset.Now;
        return ReviewEntries(topic).Where(e => e.State.IsDue(now)).Select(e => e.Id).ToList();
    }

    public int DueCount(TopicEntry topic) => DueQuestions(topic).Count;

    /// <summary>The topic has answers at all: only such topics can be weak.</summary>
    public bool HasAnswers(TopicEntry topic) => ReviewEntries(topic).Any(e => e.State.Box > 0);

    /// <summary>The nearest planned review of the topic, or null when nothing is planned.</summary>
    public DateTimeOffset? NextReview(TopicEntry topic) => ReviewsOf(topic).Min(state => state.Due);

    /// <summary>The last counted answer to some question of the topic was a problem.</summary>
    public bool HasGaps(TopicEntry topic) => ReviewsOf(topic).Any(state => state.LastWasProblem);

    /// <summary>Every answered question of the topic at the reader's level, with its answers, for the skill card.</summary>
    public IReadOnlyList<QuestionStat> QuestionStatsOf(TopicEntry topic) =>
        AnsweredQuestions(topic)
            .Select(entry => new QuestionStat(
                entry.Id,
                entry.Attempts.Count,
                entry.Attempts.Count(attempt => attempt.Outcome.IsProblem()),
                entry.Attempts[^1].Outcome,
                ReviewSchedule.StateOf(entry.Attempts)))
            .ToList();

    private IEnumerable<(string Id, ReviewState State)> ReviewEntries(TopicEntry topic) =>
        AnsweredQuestions(topic).Select(entry => (entry.Id, ReviewSchedule.StateOf(entry.Attempts))).ToList();

    private IEnumerable<(string Id, List<Attempt> Attempts)> AnsweredQuestions(TopicEntry topic)
    {
        var hidden = topic.CheckedQuestions.Where(q => !_profile.Shows(q.Level, topic.Path)).Select(q => q.Id).ToHashSet();
        var existing = topic.QuestionIds?.ToHashSet();
        var prefix = topic.Id + "/";

        // A question removed or renamed in the content cannot be asked again, so its old answers are ignored.
        return _data.Questions
            .Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(pair => (Id: pair.Key[prefix.Length..], Attempts: pair.Value))
            .Where(entry => entry.Attempts.Count > 0 && !hidden.Contains(entry.Id) && (existing is null || existing.Contains(entry.Id)));
    }

    private IJSInProcessRuntime Storage => (IJSInProcessRuntime)_js;

    private static string Key(string topicId, string questionId) => $"{topicId}/{questionId}";

    private void Save()
    {
        SaveQuietly();
        Changed?.Invoke();
    }

    /// <summary>Saves without notifying: for data that changes nothing on screen, such as the version shown.</summary>
    private void SaveQuietly() =>
        Storage.Invoke<bool>("overrideStorage.set", StorageKey, JsonSerializer.Serialize(_data, ProgressJsonContext.Default.ProgressData));
}
