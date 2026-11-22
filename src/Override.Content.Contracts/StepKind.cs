namespace Override.Content.Model;

/// <summary>Kind of a lesson step.</summary>
public enum StepKind
{
    Warmup,
    Part,
    Example,
    Practice,
    Traps,
    Summary,

    /// <summary>The topic test: questions apart from the lesson, answered without hints, with a score.</summary>
    Test,
}
