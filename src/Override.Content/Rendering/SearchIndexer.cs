using System.Net;
using System.Text.RegularExpressions;
using Override.Content.Model;
using Override.Content.Search;

namespace Override.Content.Rendering;

/// <summary>Builds the search index: the plain text of every topic with the path of its sections.</summary>
internal static partial class SearchIndexer
{
    [GeneratedRegex("<svg\\b.*?</svg>", RegexOptions.Singleline)]
    private static partial Regex Figure();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tag();

    [GeneratedRegex("\\s+")]
    private static partial Regex Spaces();

    public static SearchIndex Build(SiteIndex index, IReadOnlyList<TopicPage> pages)
    {
        var sections = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var section in index.Sections)
            Collect(section, []);

        var entries = pages
            .Select(page => new SearchEntry(page.Path, page.Title, sections.GetValueOrDefault(page.Path, ""), TextOf(page)))
            .ToList();

        return new SearchIndex(entries);

        void Collect(SectionEntry section, List<string> titles)
        {
            List<string> path = [.. titles, section.Title];
            foreach (var topic in section.Topics)
                sections[topic.Path] = string.Join(" › ", path);

            foreach (var child in section.Sections)
                Collect(child, path);
        }
    }

    /// <summary>The text of the lesson and the prompts of its questions, without markup and figures.</summary>
    private static string TextOf(TopicPage page)
    {
        var parts = page.Steps.SelectMany(step => step.Blocks.Select(block => block switch
        {
            HtmlBlock html => html.Html,
            QuestionBlock question => question.Quiz.Prompt,
            _ => "",
        }));

        var html = string.Join(' ', parts);
        var text = Tag().Replace(Figure().Replace(html, " "), " ");
        return Spaces().Replace(WebUtility.HtmlDecode(text), " ").Trim();
    }
}
