namespace Override.Content.Highlighting;

/// <summary>
/// Turns code into highlighted HTML at build time, so the site shows colored code without any script.
/// C# and SQL are colored; other languages are shown as plain text.
/// </summary>
public static class CodeHighlighter
{
    public const string CSharp = "csharp";
    public const string Sql = "sql";

    /// <summary>A complete &lt;pre&gt; block. Language is the info string of a fence, e.g. csharp or sql.</summary>
    public static string Block(string code, string? language)
    {
        var normalized = Normalize(language);
        var languageClass = normalized is null ? "" : $" class=\"language-{Html.Encode(normalized)}\"";
        return $"<pre class=\"code\"><code{languageClass}>{Highlight(code, normalized)}</code></pre>";
    }

    /// <summary>The highlighted code without the surrounding block.</summary>
    public static string Highlight(string code, string? language) => Normalize(language) switch
    {
        CSharp => CSharpHighlighter.Highlight(code),
        Sql => SqlHighlighter.Highlight(code),
        _ => Html.Encode(code),
    };

    private static string? Normalize(string? language)
    {
        var name = language?.Trim().ToLowerInvariant();
        return name switch
        {
            null or "" => null,
            "csharp" or "cs" or "c#" => CSharp,
            "sql" or "postgresql" or "pgsql" => Sql,
            _ => name,
        };
    }
}
