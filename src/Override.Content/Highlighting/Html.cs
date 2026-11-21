using System.Text;

namespace Override.Content.Highlighting;

/// <summary>Minimal HTML escaping for text inside elements. Non-ASCII text such as Cyrillic is kept as is.</summary>
internal static class Html
{
    public static string Encode(string text) => Append(new StringBuilder(text.Length), text).ToString();

    public static StringBuilder Append(StringBuilder html, ReadOnlySpan<char> text)
    {
        foreach (var c in text)
        {
            _ = c switch
            {
                '&' => html.Append("&amp;"),
                '<' => html.Append("&lt;"),
                '>' => html.Append("&gt;"),
                _ => html.Append(c),
            };
        }

        return html;
    }
}
