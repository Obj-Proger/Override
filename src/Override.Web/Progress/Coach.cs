using Override.Content.Model;
using Override.Learning;
using Override.Web.Content;

namespace Override.Web.Progress;

/// <summary>A topic that needs attention, why, and a required topic that may be the real cause.</summary>
public sealed record WeakTopic(TopicEntry Topic, double Strength, string Reason, TopicEntry? StartWith);

/// <summary>Today's review across the site: one session of questions, and how many are due in all.</summary>
public sealed record ReviewPlan(IReadOnlyList<DueQuestion> Questions, int TopicCount, int TotalDue)
{
    /// <summary>About half a minute per question.</summary>
    public int Minutes => Math.Max(1, (int)Math.Ceiling(Questions.Count * 0.5));
}

/// <summary>Simple rules that point at weak places: no guessing, only what the answers show.</summary>
public sealed class Coach(ProgressStore progress)
{
    /// <summary>Below this strength a topic is listed as weak.</summary>
    private const double WeakStrength = 0.5;

    private const int WeakTopicsShown = 5;

    public ReviewPlan PlanReview(SiteIndex index)
    {
        var due = index.AllTopics()
            .SelectMany(topic =>
            {
                var strength = progress.StrengthOf(topic);
                return progress.DueQuestions(topic).Select(id => new DueQuestion(topic.Id, id, strength));
            })
            .ToList();

        var queue = ReviewQueue.Build(due);
        return new ReviewPlan(queue, queue.Select(q => q.TopicId).Distinct().Count(), due.Count);
    }

    /// <summary>
    /// Topics with answers that are weak now: gaps in the last answers, overdue reviews or low strength.
    /// If a required topic is weak too, it is suggested first, since the trouble often starts there.
    /// </summary>
    public IReadOnlyList<WeakTopic> WeakTopics(SiteIndex index) =>
        index.AllTopics()
            .Where(progress.HasAnswers)
            .Select(topic => (Topic: topic, Strength: progress.StrengthOf(topic)))
            .Where(t => t.Strength < WeakStrength || progress.HasGaps(t.Topic))
            .OrderBy(t => t.Strength)
            .Take(WeakTopicsShown)
            .Select(t => new WeakTopic(t.Topic, t.Strength, ReasonFor(t.Topic, t.Strength), WeakRequirement(index, t.Topic)))
            .ToList();

    private string ReasonFor(TopicEntry topic, double strength)
    {
        if (progress.HasGaps(topic))
            return "есть ошибки в последних ответах";

        return progress.DueCount(topic) > 0 ? "пора повторить" : $"сила {Labels.Percent(strength)}";
    }

    private TopicEntry? WeakRequirement(SiteIndex index, TopicEntry topic) =>
        index.AllTopics()
            .Where(t => t.Id != topic.Id)
            .FirstOrDefault(t => progress.HasAnswers(t)
                && (progress.StrengthOf(t) < WeakStrength || progress.HasGaps(t))
                && topic.Requires.Contains(t.Id));
}
