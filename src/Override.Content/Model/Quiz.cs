namespace Override.Content.Model;

/// <summary>A question. Level is null when the question uses the level of its topic.</summary>
public abstract record Quiz(string Id, Level? Level, string Prompt, string? Code, string? Explain);

/// <summary>Pick one option.</summary>
public sealed record ChoiceQuiz(
    string Id, Level? Level, string Prompt, string? Code, string? Explain,
    IReadOnlyList<string> Options, int Answer)
    : Quiz(Id, Level, Prompt, Code, Explain);

/// <summary>Pick every correct option.</summary>
public sealed record MultiQuiz(
    string Id, Level? Level, string Prompt, string? Code, string? Explain,
    IReadOnlyList<string> Options, IReadOnlyList<int> Answers)
    : Quiz(Id, Level, Prompt, Code, Explain);

/// <summary>Type a value. Any of Answers is accepted.</summary>
public sealed record InputQuiz(
    string Id, Level? Level, string Prompt, string? Code, string? Explain,
    IReadOnlyList<string> Answers, bool CaseSensitive)
    : Quiz(Id, Level, Prompt, Code, Explain);

/// <summary>Type what the code prints.</summary>
public sealed record OutputQuiz(
    string Id, Level? Level, string Prompt, string? Code, string? Explain,
    IReadOnlyList<string> Answers, bool CaseSensitive)
    : Quiz(Id, Level, Prompt, Code, Explain);

/// <summary>Recall the answer, then compare with the reference and grade yourself.</summary>
public sealed record RecallQuiz(
    string Id, Level? Level, string Prompt, string? Code, string? Explain,
    string Answer)
    : Quiz(Id, Level, Prompt, Code, Explain);

/// <summary>Fill the gap marked with ___ in the code.</summary>
public sealed record FillQuiz(
    string Id, Level? Level, string Prompt, string? Code, string? Explain,
    IReadOnlyList<string> Answers, bool CaseSensitive)
    : Quiz(Id, Level, Prompt, Code, Explain);

/// <summary>Point at the line with the bug. Line counts from 1.</summary>
public sealed record FindBugQuiz(
    string Id, Level? Level, string Prompt, string? Code, string? Explain,
    int Line)
    : Quiz(Id, Level, Prompt, Code, Explain);

/// <summary>Put the lines in order. Lines are given in the correct order.</summary>
public sealed record ParsonsQuiz(
    string Id, Level? Level, string Prompt, string? Code, string? Explain,
    IReadOnlyList<string> Lines, IReadOnlyList<string> Distractors)
    : Quiz(Id, Level, Prompt, Code, Explain);

/// <summary>Write code that passes the tests.</summary>
public sealed record CodeQuiz(
    string Id, Level? Level, string Prompt, string? Code, string? Explain,
    string Language, string Starter, string Tests, string Solution)
    : Quiz(Id, Level, Prompt, Code, Explain);

/// <summary>Write a query whose result matches the reference query.</summary>
public sealed record SqlQuiz(
    string Id, Level? Level, string Prompt, string? Code, string? Explain,
    string Setup, string Solution, bool Ordered)
    : Quiz(Id, Level, Prompt, Code, Explain);
