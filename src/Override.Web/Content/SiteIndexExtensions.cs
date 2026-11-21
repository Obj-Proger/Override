using Override.Content.Model;

namespace Override.Web.Content;

/// <summary>Finding sections and topics by their address, in the order of the program.</summary>
public static class SiteIndexExtensions
{
    public static IEnumerable<SectionEntry> AllSections(this SiteIndex index) =>
        index.Sections.SelectMany(Walk);

    /// <summary>Every topic in program order: a section's own topics first, then its subsections.</summary>
    public static IEnumerable<TopicEntry> AllTopics(this SiteIndex index) =>
        index.Sections.SelectMany(TopicsIn);

    /// <summary>Topics of a section and of all its subsections, in program order.</summary>
    public static IEnumerable<TopicEntry> TopicsIn(this SectionEntry section) =>
        section.Topics.Concat(section.Sections.SelectMany(TopicsIn));

    public static SectionEntry? FindSection(this SiteIndex index, string path) =>
        index.AllSections().FirstOrDefault(s => s.Path == path);

    public static TopicEntry? FindTopic(this SiteIndex index, string path) =>
        index.AllTopics().FirstOrDefault(t => t.Path == path);

    public static TopicEntry? FindTopicById(this SiteIndex index, string id) =>
        index.AllTopics().FirstOrDefault(t => t.Id == id);

    /// <summary>The topic after this one in the program, or null for the last one.</summary>
    public static TopicEntry? NextTopic(this SiteIndex index, string id) =>
        index.AllTopics().SkipWhile(t => t.Id != id).Skip(1).FirstOrDefault();

    /// <summary>The sections that contain the address, from the top: stack, technology, section and so on.</summary>
    public static IReadOnlyList<SectionEntry> Trail(this SiteIndex index, string path) =>
        index.AllSections()
            .Where(s => path == s.Path || path.StartsWith(s.Path + "/", StringComparison.Ordinal))
            .OrderBy(s => s.Path.Length)
            .ToList();

    private static IEnumerable<SectionEntry> Walk(SectionEntry section) =>
        section.Sections.SelectMany(Walk).Prepend(section);
}
