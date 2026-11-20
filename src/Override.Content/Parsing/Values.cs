using System.Globalization;
using System.Text.RegularExpressions;
using Override.Content.Model;

namespace Override.Content.Parsing;

/// <summary>Conversions and patterns shared by the readers.</summary>
internal static partial class Values
{
    /// <summary>Topic id: dot-separated lower-case segments, at least two, e.g. csharp.types.boxing.</summary>
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*(\\.[a-z0-9]+(-[a-z0-9]+)*)+$")]
    public static partial Regex TopicId();

    /// <summary>Question id: lower-case words joined by hyphens, e.g. boxing-copy.</summary>
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    public static partial Regex QuizId();

    public static bool TryParseLevel(string? value, out Level level)
    {
        switch (value)
        {
            case "junior":
                level = Level.Junior;
                return true;
            case "middle":
                level = Level.Middle;
                return true;
            case "senior":
                level = Level.Senior;
                return true;
            default:
                level = default;
                return false;
        }
    }

    public static bool TryParseTrack(string? value, out Track track)
    {
        switch (value)
        {
            case "backend":
                track = Track.Backend;
                return true;
            case "frontend":
                track = Track.Frontend;
                return true;
            case "fullstack":
                track = Track.Fullstack;
                return true;
            default:
                track = default;
                return false;
        }
    }

    /// <summary>A YAML scalar read into object arrives as a string; numbers are parsed here.</summary>
    public static bool TryParseInt(object? value, out int number)
    {
        number = 0;
        return value is string text
            && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number);
    }

    /// <summary>A single string or a list of strings.</summary>
    public static IReadOnlyList<string>? AsStrings(object? value)
    {
        if (value is string single)
            return [single];

        if (value is IEnumerable<object> items)
        {
            var list = new List<string>();
            foreach (var item in items)
            {
                if (item is not string text)
                    return null;

                list.Add(text);
            }

            return list;
        }

        return null;
    }

    /// <summary>A list of non-negative integers.</summary>
    public static IReadOnlyList<int>? AsInts(object? value)
    {
        if (value is not IEnumerable<object> items)
            return null;

        var list = new List<int>();
        foreach (var item in items)
        {
            if (!TryParseInt(item, out var number))
                return null;

            list.Add(number);
        }

        return list;
    }

    /// <summary>Block scalars end with a line break; it is not part of the value.</summary>
    public static string? TrimBlock(string? value) => value?.TrimEnd('\r', '\n');
}
