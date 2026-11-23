using System.Text.RegularExpressions;

namespace Override.Content.Highlighting;

/// <summary>Colors XML, such as .csproj files, like Visual Studio: delimiters, element names, attributes and values.</summary>
internal static partial class XmlHighlighter
{
    [GeneratedRegex("(?<cmt><!--.*?-->)|(?<tag><[^>]*>)", RegexOptions.Singleline)]
    private static partial Regex Token();

    [GeneratedRegex("^</?\\??(?<name>[\\w:.-]+)")]
    private static partial Regex ElementName();

    [GeneratedRegex("(?<attr>[\\w:.-]+)\\s*=\\s*(?<value>\"[^\"]*\"|'[^']*')")]
    private static partial Regex Attribute();

    public static string Highlight(string code)
    {
        var classes = new string?[code.Length];

        foreach (Match match in Token().Matches(code))
        {
            if (match.Groups["cmt"].Success)
            {
                Mark(classes, match.Index, match.Length, "cmt");
                continue;
            }

            var tag = match.Value;
            Mark(classes, match.Index, match.Length, "xml-delim");

            var name = ElementName().Match(tag);
            if (name.Success)
            {
                var group = name.Groups["name"];
                Mark(classes, match.Index + group.Index, group.Length, "xml-name");
            }

            foreach (Match attribute in Attribute().Matches(tag, name.Success ? name.Length : 0))
            {
                var attr = attribute.Groups["attr"];
                var value = attribute.Groups["value"];
                Mark(classes, match.Index + attr.Index, attr.Length, "xml-attr");
                Mark(classes, match.Index + value.Index, value.Length, "xml-val");
            }
        }

        return TokenRuns.ToHtml(code, classes);
    }

    private static void Mark(string?[] classes, int start, int length, string css)
    {
        for (var i = start; i < start + length; i++)
            classes[i] = css;
    }
}
