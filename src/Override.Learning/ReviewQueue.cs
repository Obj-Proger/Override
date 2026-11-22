namespace Override.Learning;

/// <summary>A question due for review, with the strength of its topic.</summary>
public sealed record DueQuestion(string TopicId, string QuestionId, double TopicStrength);

/// <summary>
/// Orders today's review: weaker topics first, and topics interleaved rather than one after another,
/// since mixing topics makes recall harder and so trains it better. The limit keeps a missed week
/// from turning into a pile of questions.
/// </summary>
public static class ReviewQueue
{
    public const int DefaultLimit = 15;

    public static IReadOnlyList<DueQuestion> Build(IEnumerable<DueQuestion> due, int limit = DefaultLimit)
    {
        var topics = due
            .GroupBy(q => q.TopicId)
            .OrderBy(g => g.First().TopicStrength)
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new Queue<DueQuestion>(g))
            .ToList();

        var result = new List<DueQuestion>();
        while (result.Count < limit && topics.Count > 0)
        {
            // One question from each topic per round.
            foreach (var topic in topics.ToList())
            {
                if (result.Count == limit)
                    break;

                result.Add(topic.Dequeue());
                if (topic.Count == 0)
                    topics.Remove(topic);
            }
        }

        return result;
    }
}
