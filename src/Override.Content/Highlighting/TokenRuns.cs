using System.Text;

namespace Override.Content.Highlighting;

/// <summary>
/// Writes code as HTML where each character may have a CSS class; neighbours with the same class share a span.
/// A span never crosses a line break, so the result can be split into lines.
/// </summary>
internal static class TokenRuns
{
    public static string ToHtml(string code, IReadOnlyList<string?> classes)
    {
        var html = new StringBuilder(code.Length * 2);
        var start = 0;

        while (start < code.Length)
        {
            var css = classes[start];
            var end = start + 1;
            while (end < code.Length && classes[end] == css)
                end++;

            var text = code.AsSpan(start, end - start);
            if (css is null)
            {
                Html.Append(html, text);
            }
            else
            {
                var first = true;
                foreach (var line in text.ToString().Split('\n'))
                {
                    if (!first)
                        html.Append('\n');

                    first = false;
                    if (line.Length > 0)
                    {
                        html.Append("<span class=\"c-").Append(css).Append("\">");
                        Html.Append(html, line).Append("</span>");
                    }
                }
            }

            start = end;
        }

        return html.ToString();
    }
}
