namespace Override.Learning;

/// <summary>How an attempt at a question ended.</summary>
public enum Outcome
{
    Correct,
    Wrong,

    /// <summary>Self-assessment after seeing the reference: did not recall. Counts as a problem.</summary>
    Forgot,

    /// <summary>Self-assessment: recalled part of it. Counts as a problem.</summary>
    Partial,

    /// <summary>Self-assessment: recalled. Never counts as knowledge: the user's own estimate can be too kind.</summary>
    Recalled,
}

/// <summary>
/// One answer. First is true when the question had no answer in the hours before: only such attempts
/// show what the user knows, retries after reading the explanation do not.
/// </summary>
public sealed record Attempt(DateTimeOffset At, Outcome Outcome, bool First);

public static class OutcomeExtensions
{
    /// <summary>A wrong answer or a self-assessment that admits a gap.</summary>
    public static bool IsProblem(this Outcome outcome) => outcome is Outcome.Wrong or Outcome.Forgot or Outcome.Partial;
}
