namespace Override.Web.Content;

/// <summary>
/// Links to report an inaccuracy: a new GitHub issue with the place already filled in, so the reader only has
/// to say what is wrong.
/// </summary>
public static class Feedback
{
    public const string Repository = "https://github.com/Obj-Proger/Override";

    public static string Issue(string title, string place) =>
        $"{Repository}/issues/new?title={Uri.EscapeDataString(title)}&body={Uri.EscapeDataString(Body(place))}";

    private static string Body(string place) => $"""
        {place}

        Что не так:


        Как должно быть, если знаете:

        """;
}
