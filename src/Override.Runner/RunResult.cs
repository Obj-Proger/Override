namespace Override.Runner;

/// <summary>How a run ended.</summary>
public enum RunStatus
{
    /// <summary>The code does not compile; Messages lists the errors.</summary>
    CompileError,

    /// <summary>Every check passed.</summary>
    Passed,

    /// <summary>A check failed; Messages holds the check and what was expected.</summary>
    Failed,

    /// <summary>The code threw an exception that no check expected.</summary>
    Crashed,

    /// <summary>The code ran too long or recursed too deep and was stopped.</summary>
    Stopped,
}

/// <summary>The result of a run: its status, messages for the reader, and what the code printed.</summary>
public sealed record RunResult(RunStatus Status, IReadOnlyList<string> Messages, string Output)
{
    public bool Passed => Status == RunStatus.Passed;
}

/// <summary>
/// Limits that keep a run from freezing the page: loop iterations in total, recursion depth, time and
/// the length of the printed output.
/// </summary>
public sealed record RunLimits(long MaxIterations, int MaxDepth, TimeSpan Time, int MaxOutput)
{
    public static readonly RunLimits Default = new(MaxIterations: 5_000_000, MaxDepth: 256, Time: TimeSpan.FromSeconds(3), MaxOutput: 10_000);
}
