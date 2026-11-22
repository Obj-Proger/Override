using System.Globalization;
using System.Text;
using Override.Content.Model;
using Override.Web.Content;

namespace Override.Web.Progress;

/// <summary>A topic in the skill card.</summary>
public sealed record SkillTopic(TopicEntry Topic, TopicStatus Status, double Strength, bool HasGaps, int DueCount, TestResult? BestTest);

/// <summary>A section in the skill card: its path of titles and its topics.</summary>
public sealed record SkillSection(string Title, IReadOnlyList<SkillTopic> Topics);

/// <summary>A stack in the skill card with the reader's level for it.</summary>
public sealed record SkillStack(SectionEntry Stack, Level Level, IReadOnlyList<SkillSection> Sections);

/// <summary>
/// A summary of what the reader knows, built from the same data as the rest of the site. It can be copied
/// as Markdown, for oneself or for an assistant that helps with further study.
/// </summary>
public sealed class SkillCard(ProgressStore progress, ProfileStore profile)
{
    /// <summary>From this strength a topic is listed among the strong ones.</summary>
    public const double StrongStrength = 0.7;

    public IReadOnlyList<SkillStack> Build(SiteIndex index) =>
        index.Sections.Select(stack => new SkillStack(stack, profile.LevelFor(stack.Path), SectionsOf(stack))).ToList();

    /// <summary>Topics grouped by their deepest section, titled with the path below the stack.</summary>
    private List<SkillSection> SectionsOf(SectionEntry stack)
    {
        var sections = new List<SkillSection>();
        Collect(stack, []);
        return sections;

        void Collect(SectionEntry section, List<string> titles)
        {
            if (section.Topics.Count > 0)
            {
                var title = titles.Count > 0 ? string.Join(" → ", titles) : section.Title;
                sections.Add(new SkillSection(title, section.Topics.Select(Describe).ToList()));
            }

            foreach (var child in section.Sections)
                Collect(child, [.. titles, child.Title]);
        }
    }

    private SkillTopic Describe(TopicEntry topic) => new(
        topic,
        progress.StatusOf(topic),
        progress.StrengthOf(topic),
        progress.HasGaps(topic),
        progress.DueCount(topic),
        progress.BestTest(topic.Id));

    public string ToMarkdown(IReadOnlyList<SkillStack> stacks, DateTimeOffset now)
    {
        var text = new StringBuilder();
        text.Append("# Сводка навыков, ").AppendLine(now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture));
        if (profile.Profile is { } chosen)
            text.Append("Направление: ").AppendLine(Labels.Of(chosen.Track));

        foreach (var stack in stacks)
        {
            text.AppendLine().Append("## ").Append(stack.Stack.Title).Append(" — уровень ").AppendLine(Labels.Of(stack.Level));
            foreach (var section in stack.Sections)
            {
                text.AppendLine().Append("### ").AppendLine(section.Title);
                foreach (var topic in section.Topics)
                    text.Append("- ").AppendLine(Line(topic));
            }
        }

        var all = stacks.SelectMany(s => s.Sections).SelectMany(s => s.Topics).ToList();
        AppendList(text, "Сильные стороны", all.Where(t => t.Strength >= StrongStrength && !t.HasGaps));
        AppendList(text, "Слабые места", all.Where(t => t.Status != TopicStatus.NotStarted && (t.HasGaps || t.Strength < 0.5)));
        AppendList(text, "Не начато", all.Where(t => t.Status == TopicStatus.NotStarted));
        return text.ToString();
    }

    public static string StatusText(TopicStatus status) => status switch
    {
        TopicStatus.Done => "пройдена",
        TopicStatus.Started => "изучается",
        _ => "не начата",
    };

    private static string Line(SkillTopic topic)
    {
        var parts = new List<string> { StatusText(topic.Status) };
        if (topic.Status != TopicStatus.NotStarted)
            parts.Add($"сила {Labels.Percent(topic.Strength)}");
        if (topic.BestTest is { } test)
            parts.Add($"тест {Labels.Percent(test.Share)}");
        if (topic.HasGaps)
            parts.Add("есть ошибки в последних ответах");

        return $"{topic.Topic.Title} ({Labels.Of(topic.Topic.Level)}): {string.Join(", ", parts)}";
    }

    private static void AppendList(StringBuilder text, string title, IEnumerable<SkillTopic> topics)
    {
        var list = topics.ToList();
        if (list.Count == 0)
            return;

        text.AppendLine().Append("## ").AppendLine(title);
        foreach (var topic in list)
            text.Append("- ").AppendLine(topic.Topic.Title);
    }
}
