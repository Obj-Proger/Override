namespace Override.Learning;

/// <summary>
/// How well something is known right now, from 0 to 1. It grows with right answers spread over time
/// and fades when a review is overdue, so a topic not repeated for long shows as needing attention.
/// </summary>
public static class Strength
{
    /// <summary>Strength right after a right answer in each box: one right answer is a start, not knowledge.</summary>
    private static readonly double[] _boxStrength = [0.4, 0.6, 0.75, 0.9, 1.0];

    public static double OfQuestion(ReviewState state, DateTimeOffset now)
    {
        if (state.Box == 0 || state.LastWasProblem || state.LastReview is not { } last)
            return 0;

        var value = _boxStrength[state.Box - 1];
        var interval = ReviewSchedule.IntervalOf(state.Box);
        var overdue = now - (last + interval);

        // Past the planned review, strength halves with every further interval.
        return overdue <= TimeSpan.Zero ? value : value * Math.Pow(0.5, overdue / interval);
    }

    /// <summary>The average over the questions; questions never answered count as 0.</summary>
    public static double OfTopic(IEnumerable<ReviewState> questions, DateTimeOffset now)
    {
        var values = questions.Select(q => OfQuestion(q, now)).ToList();
        return values.Count == 0 ? 0 : values.Average();
    }
}
