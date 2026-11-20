using Override.Content.Model;

namespace Override.Content.Parsing;

/// <summary>Result of parsing one topic file. Topic is null when there are errors.</summary>
public sealed record ParseResult(Topic? Topic, IReadOnlyList<ContentDiagnostic> Diagnostics)
{
    public bool Succeeded => Topic is not null;
}
