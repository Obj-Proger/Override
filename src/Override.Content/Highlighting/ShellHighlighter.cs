using System.Text.RegularExpressions;

namespace Override.Content.Highlighting;

/// <summary>
/// Colors terminal commands: the program at the start of a line, options, quoted strings and comments.
/// </summary>
internal static partial class ShellHighlighter
{
    [GeneratedRegex(
        "(?<cmt>(?<=^|\\s)#[^\\n]*)|(?<str>\"[^\"\\n]*\"|'[^'\\n]*')|(?<cmd>(?<=^[ \\t]*)[^\\s\"'#]+)|(?<opt>(?<=\\s)--?[A-Za-z][\\w-]*)",
        RegexOptions.Multiline)]
    private static partial Regex Token();

    public static string Highlight(string code)
    {
        var classes = new string?[code.Length];

        foreach (Match match in Token().Matches(code))
        {
            var css = match.Groups["cmt"].Success ? "cmt"
                : match.Groups["str"].Success ? "str"
                : match.Groups["cmd"].Success ? "method"
                : "kw";

            for (var i = match.Index; i < match.Index + match.Length; i++)
                classes[i] = css;
        }

        return TokenRuns.ToHtml(code, classes);
    }
}
