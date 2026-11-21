using System.Text;

namespace Override.Content.Highlighting;

/// <summary>Writes code as HTML where each character may have a CSS class; neighbours with the same class share a span.</summary>
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
                html.Append("<span class=\"c-").Append(css).Append("\">");
                Html.Append(html, text).Append("</span>");
            }

            start = end;
        }

        return html.ToString();
    }
}
