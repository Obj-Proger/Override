using Override.Content.Model;

namespace Override.Web.Content;

/// <summary>Names shown on the site. Russian for now; they move to resources with the translation.</summary>
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
        StepKind.Part => "Часть",
        StepKind.Example => "Пример",
        StepKind.Practice => "Практика",
        StepKind.Traps => "Ловушки",
        StepKind.Summary => "Итог",
        _ => kind.ToString(),
    };

    public static string Minutes(int minutes) => $"{minutes} мин";
}
