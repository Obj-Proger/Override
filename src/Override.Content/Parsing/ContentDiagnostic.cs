namespace Override.Content.Parsing;

/// <summary>An error in a content file, in the "file(line): message" form that MSBuild and IDEs understand.</summary>
public sealed record ContentDiagnostic(string File, int Line, string Message)
{
    public override string ToString() => $"{File}({Line}): error: {Message}";
}
