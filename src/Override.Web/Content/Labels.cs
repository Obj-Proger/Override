using Override.Content.Model;

namespace Override.Web.Content;

/// <summary>Texts shown on the site. Russian for now; they move to resources with the translation.</summary>
public static class Labels
{
    public static string Of(Level level) => level switch
    {
        Level.Junior => "Junior",
        Level.Middle => "Middle",
        Level.Senior => "Senior",
        _ => level.ToString(),
    };

    public static string Of(StepKind kind) => kind switch
    {
        StepKind.Warmup => "Разминка",
        StepKind.Part => "Объяснение",
        StepKind.Example => "Пример",
        StepKind.Practice => "Практика",
        StepKind.Traps => "Ловушки",
        StepKind.Summary => "Итог",
        _ => kind.ToString(),
    };

    /// <summary>The name of a step: its own title when it has one.</summary>
    public static string Of(PageStep step) => step.Title ?? Of(step.Kind);

    /// <summary>What the question asks the user to do.</summary>
    public static string Of(Quiz quiz) => quiz switch
    {
        ChoiceQuiz => "выбери ответ",
        MultiQuiz => "выбери все верные",
        InputQuiz => "введи ответ",
        OutputQuiz => "что выведет код",
        RecallQuiz => "ответь своими словами",
        FillQuiz => "заполни пропуск",
        FindBugQuiz => "найди ошибку",
        ParsonsQuiz => "собери код",
        CodeQuiz => "напиши код",
        SqlQuiz => "напиши запрос",
        _ => "вопрос",
    };

    /// <summary>Short form for lists: "20 мин".</summary>
    public static string Minutes(int minutes) => $"{minutes} мин";

    /// <summary>Full form for a topic: "20 минут".</summary>
    public static string MinutesLong(int minutes) => $"{minutes} {Plural(minutes, "минута", "минуты", "минут")}";

    public static string Topics(int count) => $"{count} {Plural(count, "тема", "темы", "тем")}";

    /// <summary>Russian plural: 1 тема, 2 темы, 5 тем, 21 тема.</summary>
    public static string Plural(int count, string one, string few, string many)
    {
        var lastTwo = Math.Abs(count) % 100;
        var last = lastTwo % 10;

        if (lastTwo is >= 11 and <= 14)
            return many;

        return last switch
        {
            1 => one,
            >= 2 and <= 4 => few,
            _ => many,
        };
    }
}
