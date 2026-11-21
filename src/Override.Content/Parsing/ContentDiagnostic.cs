namespace Override.Content.Parsing;

/// <summary>How serious a content problem is. Errors stop the build; warnings do not.</summary>
public enum DiagnosticSeverity
{
    Error,
    Warning,
}

/// <summary>A problem in a content file, in the "file(line): error: message" form that MSBuild and IDEs understand.</summary>
public sealed record ContentDiagnostic(
    string File,
    int Line,
    string Message,
    DiagnosticSeverity Severity = DiagnosticSeverity.Error)
{
    public override string ToString() =>
        $"{File}({Line}): {(Severity == DiagnosticSeverity.Error ? "error" : "warning")}: {Message}";
}
