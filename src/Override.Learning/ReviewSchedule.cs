namespace Override.Learning;

/// <summary>
/// Where a question stands in repetition. Box 0 means never answered; boxes 1 to 5 mean ever longer
/// intervals. Due is when the question should be asked again; null when nothing is planned.
/// </summary>
public sealed record ReviewState(int Box, DateTimeOffset? LastReview, DateTimeOffset? Due, bool LastWasProblem)
{
    public static readonly ReviewState New = new(0, null, null, false);

    public bool IsDue(DateTimeOffset now) => Due is { } due && due <= now;
}

/// <summary>
/// Leitner boxes. A right first attempt moves a question one box up, a problem sends it back to box 1,
/// and each box has a longer interval before the question comes back: 1, 3, 7, 16 and 35 days.
/// The state is computed from the attempts, so nothing besides the attempts has to be stored.
/// </summary>
public static class ReviewSchedule
{
    public const int MaxBox = 5;

    private static readonly int[] _intervalDays = [1, 3, 7, 16, 35];

    public static TimeSpan IntervalOf(int box) => TimeSpan.FromDays(_intervalDays[Math.Clamp(box, 1, MaxBox) - 1]);

    public static ReviewState StateOf(IEnumerable<Attempt> attempts)
    {
        var state = ReviewState.New;

        foreach (var attempt in attempts.OrderBy(a => a.At))
        {
            // "Recalled" proves nothing, but it closes a pending review of a recall question:
            // otherwise a question once forgotten would stay due forever.
            if (attempt.Outcome == Outcome.Recalled)
            {
                if (state.LastWasProblem)
                    state = state with { Due = null, LastWasProblem = false };

                continue;
            }

            // Retries right after an answer tell nothing new. An admitted gap always counts.
            var counts = attempt.First || attempt.Outcome is Outcome.Forgot or Outcome.Partial;
            if (!counts)
                continue;

            var problem = attempt.Outcome.IsProblem();
            var box = problem ? 1 : Math.Min(state.Box + 1, MaxBox);
            state = new ReviewState(box, attempt.At, attempt.At + IntervalOf(box), problem);
        }

        return state;
    }
}
