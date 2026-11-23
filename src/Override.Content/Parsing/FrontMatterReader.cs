using Override.Content.Model;

namespace Override.Content.Parsing;

/// <summary>Reads and validates the front matter of a topic file.</summary>
internal static class FrontMatterReader
{
    private const int MaxMinutes = 240;
    private const int MaxTestMinutes = 60;
    private const int MaxTestSize = 30;

    public static TopicMetadata? Read(string yaml, string file, int firstLine, ICollection<ContentDiagnostic> diagnostics)
    {
        var document = Yaml.Read<FrontMatterDocument>(yaml, file, firstLine, diagnostics);
        if (document is null)
            return null;

        var errorsBefore = diagnostics.Count;
        void Error(string message) => diagnostics.Add(new(file, firstLine, message));

        if (string.IsNullOrWhiteSpace(document.Id) || !Values.TopicId().IsMatch(document.Id))
            Error("'id' is required and must look like 'csharp.types.boxing'.");

        if (string.IsNullOrWhiteSpace(document.Title))
            Error("'title' is required.");

        if (!Values.TryParseLevel(document.Level, out var level))
            Error("'level' is required: junior, middle or senior.");

        var tracks = new List<Track>();
        if (document.Tracks is null || document.Tracks.Count == 0)
        {
            Error("'tracks' is required: one or more of backend, frontend, fullstack.");
        }
        else
        {
            foreach (var value in document.Tracks)
            {
                if (!Values.TryParseTrack(value, out var track))
                    Error($"Unknown track '{value}': use backend, frontend or fullstack.");
                else if (tracks.Contains(track))
                    Error($"Track '{value}' is listed twice.");
                else
                    tracks.Add(track);
            }
        }

        if (document.Minutes is not (> 0 and <= MaxMinutes))
            Error($"'minutes' is required: from 1 to {MaxMinutes}.");

        if (document.Order is not >= 0)
            Error("'order' is required: 0 or more.");

        var requires = document.Requires ?? [];
        foreach (var id in requires)
        {
            if (!Values.TopicId().IsMatch(id))
                Error($"'requires' contains '{id}', which is not a topic id.");
            else if (id == document.Id)
                Error("A topic cannot require itself.");
        }

        if (requires.Distinct().Count() != requires.Count)
            Error("'requires' lists a topic twice.");

        var quickCheck = document.QuickCheck ?? [];
        if (quickCheck.Distinct().Count() != quickCheck.Count)
            Error("'quickCheck' lists a question twice.");

        if (document.TestMinutes is { } testMinutes && testMinutes is < 1 or > MaxTestMinutes)
            Error($"'testMinutes' must be from 1 to {MaxTestMinutes}.");

        if (document.TestSize is { } testSize && testSize is < 2 or > MaxTestSize)
            Error($"'testSize' must be from 2 to {MaxTestSize}.");

        if (diagnostics.Count != errorsBefore)
            return null;

        return new TopicMetadata(
            document.Id!,
            document.Title!.Trim(),
            level,
            tracks,
            document.Minutes!.Value,
            document.Order!.Value,
            requires,
            quickCheck,
            document.TestMinutes,
            document.TestSize);
    }

    /// <summary>The front matter as written in YAML.</summary>
    internal sealed class FrontMatterDocument
    {
        public string? Id { get; set; }

        public string? Title { get; set; }

        public string? Level { get; set; }

        public List<string>? Tracks { get; set; }

        public int? Minutes { get; set; }

        public int? Order { get; set; }

        public List<string>? Requires { get; set; }

        public List<string>? QuickCheck { get; set; }

        public int? TestMinutes { get; set; }

        public int? TestSize { get; set; }
    }
}
