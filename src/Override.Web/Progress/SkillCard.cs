using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Override.Content.Model;
using Override.Learning;
using Override.Web.Content;

namespace Override.Web.Progress;

/// <summary>
/// A question the reader made mistakes on: its text and code as HTML, and its answers. Fixed means the last
/// counted answer was right, so the mistake is in the past.
/// </summary>
public sealed record SkillQuestion(string Id, string PromptHtml, string? CodeHtml, QuestionStat Stat)
{
    public bool Fixed => !Stat.Review.LastWasProblem;
}

/// <summary>A topic in the skill card, with how many questions were answered and the ones with mistakes.</summary>
public sealed record SkillTopic(
    TopicEntry Topic,
    TopicStatus Status,
    double Strength,
    bool HasGaps,
    int DueCount,
    TestResult? BestTest,
    int Answered,
    IReadOnlyList<SkillQuestion> Mistakes);

/// <summary>A section in the skill card: its path of titles and its topics.</summary>
public sealed record SkillSection(string Title, IReadOnlyList<SkillTopic> Topics);

/// <summary>A stack in the skill card with the reader's level for it.</summary>
public sealed record SkillStack(SectionEntry Stack, Level Level, IReadOnlyList<SkillSection> Sections);

/// <summary>
/// A summary of what the reader knows, built from the same data as the rest of the site. It can be copied
/// as Markdown, for oneself or for an assistant that helps with further study.
/// </summary>
public sealed partial class SkillCard(ProgressStore progress, ProfileStore profile)
{
    /// <summary>From this strength a topic is listed among the strong ones.</summary>
    public const double StrongStrength = 0.7;

    /// <param name="pages">Loaded topic pages by id, for the text of questions; a topic without one lists no texts.</param>
    public IReadOnlyList<SkillStack> Build(SiteIndex index, IReadOnlyDictionary<string, TopicPage> pages) =>
        index.Sections.Select(stack => new SkillStack(stack, profile.LevelFor(stack.Path), SectionsOf(stack, pages))).ToList();

    /// <summary>Topics grouped by their deepest section, titled with the path below the stack.</summary>
    private List<SkillSection> SectionsOf(SectionEntry stack, IReadOnlyDictionary<string, TopicPage> pages)
    {
        var sections = new List<SkillSection>();
        Collect(stack, []);
        return sections;

        void Collect(SectionEntry section, List<string> titles)
        {
            if (section.Topics.Count > 0)
            {
                var title = titles.Count > 0 ? string.Join(" → ", titles) : section.Title;
                sections.Add(new SkillSection(title, section.Topics.Select(topic => Describe(topic, pages)).ToList()));
            }

            foreach (var child in section.Sections)
                Collect(child, [.. titles, child.Title]);
        }
    }

    private SkillTopic Describe(TopicEntry topic, IReadOnlyDictionary<string, TopicPage> pages)
    {
        var stats = progress.QuestionStatsOf(topic);
        pages.TryGetValue(topic.Id, out var page);

        // Unfixed mistakes first, then the most frequent ones.
        var mistakes = stats
            .Where(stat => stat.Problems > 0)
            .Select(stat => Question(stat, page))
            .OrderBy(question => question.Fixed)
            .ThenByDescending(question => question.Stat.Problems)
            .ToList();

        return new SkillTopic(
            topic,
            progress.StatusOf(topic),
            progress.StrengthOf(topic),
            progress.HasGaps(topic),
            progress.DueCount(topic),
            progress.BestTest(topic.Id),
            stats.Count,
            mistakes);
    }

    private static SkillQuestion Question(QuestionStat stat, TopicPage? page)
    {
        var block = page?.Steps.SelectMany(step => step.Blocks).OfType<QuestionBlock>().FirstOrDefault(b => b.Quiz.Id == stat.Id)
            ?? page?.Test.FirstOrDefault(b => b.Quiz.Id == stat.Id);

        return new SkillQuestion(stat.Id, block?.Quiz.Prompt ?? stat.Id, block?.CodeHtml, stat);
    }

    public static string OutcomeText(Outcome outcome) => outcome switch
    {
        Outcome.Correct => "верно",
        Outcome.Wrong => "неверно",
        Outcome.Forgot => "не вспомнил",
        Outcome.Partial => "вспомнил частично",
        _ => "вспомнил",
    };

    /// <summary>"ошибок 2 из 3 ответов, последний — неверно".</summary>
    public static string StatText(QuestionStat stat) =>
        $"ошибок {stat.Problems} из {stat.Answers}, последний ответ — {OutcomeText(stat.Last)}";

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
                {
                    text.Append("- ").AppendLine(Line(topic));
                    AppendMistakes(text, topic);
                }
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

    /// <summary>Each question with mistakes: its text, its code and the answers, so a reader or an assistant sees the gap.</summary>
    private static void AppendMistakes(StringBuilder text, SkillTopic topic)
    {
        if (topic.Answered == 0)
            return;

        text.Append("  - отвечено вопросов: ").Append(topic.Answered)
            .Append(", с ошибками: ").Append(topic.Mistakes.Count).AppendLine();

        foreach (var question in topic.Mistakes)
        {
            text.Append("  - ").Append(question.Fixed ? "[исправлено] " : "[не исправлено] ")
                .Append(PlainText(question.PromptHtml)).Append(" (").Append(StatText(question.Stat)).AppendLine(")");

            if (question.CodeHtml is { } code)
            {
                text.AppendLine("    ```csharp");
                foreach (var line in PlainCode(code).Split('\n'))
                    text.Append("    ").AppendLine(line);
                text.AppendLine("    ```");
            }
        }
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tag();

    [GeneratedRegex("\\s+")]
    private static partial Regex Spaces();

    private static string PlainText(string html) => Spaces().Replace(WebUtility.HtmlDecode(Tag().Replace(html, " ")), " ").Trim();

    private static string PlainCode(string html) => WebUtility.HtmlDecode(Tag().Replace(html, "")).TrimEnd();

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
