using System.Text;

namespace Override.Content.Search;

/// <summary>
/// Search over topic titles, sections and text. Every word of the query must be found; a title match ranks
/// above a section match, which ranks above a text match. Letter case and "ё" do not matter.
/// </summary>
public static class SiteSearch
{
    public const int MaxHits = 20;
    private const int SnippetRadius = 60;

    public static IReadOnlyList<SearchHit> Find(SearchIndex index, string query)
    {
        var words = Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return [];

        return index.Entries
            .Select(entry => (Entry: entry, Score: Score(entry, words)))
            .Where(result => result.Score > 0)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Entry.Title, StringComparer.Ordinal)
            .Take(MaxHits)
            .Select(result => new SearchHit(result.Entry, Snippet(result.Entry, words)))
            .ToList();
    }

    /// <summary>Lower case, "ё" as "е", runs of spaces as one space.</summary>
    public static string Normalize(string text)
    {
        var result = new StringBuilder(text.Length);
        var space = false;

        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                space = result.Length > 0;
                continue;
            }

            if (space)
                result.Append(' ');

            space = false;
            var lower = char.ToLowerInvariant(c);
            result.Append(lower == 'ё' ? 'е' : lower);
        }

        return result.ToString();
    }

    /// <summary>0 when some word is missing; otherwise higher for words in the title and the section.</summary>
    private static int Score(SearchEntry entry, string[] words)
    {
        var title = Normalize(entry.Title);
        var section = Normalize(entry.Section);
        var text = Normalize(entry.Text);
        var score = 0;

        foreach (var word in words)
        {
            if (title.Contains(word, StringComparison.Ordinal))
                score += 100;
            else if (section.Contains(word, StringComparison.Ordinal))
                score += 10;
            else if (text.Contains(word, StringComparison.Ordinal))
                score += 1;
            else
                return 0;
        }

        return score;
    }

    /// <summary>Text around the first word found in the text, or nothing when every word is in the title.</summary>
    private static string Snippet(SearchEntry entry, string[] words)
    {
        var text = Normalize(entry.Text);
        var position = words
            .Select(word => text.IndexOf(word, StringComparison.Ordinal))
            .Where(index => index >= 0)
            .DefaultIfEmpty(-1)
            .Min();

        if (position < 0)
            return "";

        // The normalized text has the same characters as the original apart from spaces and case,
        // so the snippet is cut from the normalized original with its case restored.
        var original = string.Join(' ', entry.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var start = Math.Max(0, position - SnippetRadius);
        var end = Math.Min(original.Length, position + SnippetRadius);
        var snippet = original[start..end].Trim();

        return (start > 0 ? "…" : "") + snippet + (end < original.Length ? "…" : "");
    }
}
