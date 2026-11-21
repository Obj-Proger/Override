using Override.Content.Model;

namespace Override.Web.Content;

/// <summary>Finding sections and topics by their address.</summary>
public static class SiteIndexExtensions
{
    public static IEnumerable<SectionEntry> AllSections(this SiteIndex index) =>
        index.Sections.SelectMany(Walk);

    public static SectionEntry? FindSection(this SiteIndex index, string path) =>
        index.AllSections().FirstOrDefault(s => s.Path == path);

    public static TopicEntry? FindTopic(this SiteIndex index, string path) =>
        index.AllSections().SelectMany(s => s.Topics).FirstOrDefault(t => t.Path == path);

    private static IEnumerable<SectionEntry> Walk(SectionEntry section) =>
        section.Sections.SelectMany(Walk).Prepend(section);
}
