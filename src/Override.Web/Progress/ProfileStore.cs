using System.Text.Json;
using Microsoft.JSInterop;
using Override.Content.Model;

namespace Override.Web.Progress;

/// <summary>
/// The level and direction the reader chose. StackLevels overrides the level for single stacks, keyed by the
/// stack's address (the first part of a page address, e.g. dotnet): one can be Middle in C# and Junior in frontend.
/// </summary>
public sealed record LearnerProfile(Level Level, Track Track, IReadOnlyDictionary<string, Level>? StackLevels = null);

/// <summary>Keeps the reader's choice in local storage. Until the reader chooses, Profile is null.</summary>
public sealed class ProfileStore(IJSRuntime js)
{
    private const string StorageKey = "override.profile";

    public LearnerProfile? Profile { get; private set; }

    public event Action? Changed;

    public void Load()
    {
        var json = ((IJSInProcessRuntime)js).Invoke<string?>("overrideStorage.get", StorageKey);
        if (string.IsNullOrEmpty(json))
            return;

        try
        {
            Profile = JsonSerializer.Deserialize(json, ProgressJsonContext.Default.LearnerProfile);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException or ArgumentException)
        {
            // Damaged or outdated data: the reader is asked again.
            Console.Error.WriteLine($"Saved profile was not read: {exception.Message}");
        }
    }

    public void Set(LearnerProfile profile)
    {
        Profile = profile;
        ((IJSInProcessRuntime)js).Invoke<bool>(
            "overrideStorage.set", StorageKey, JsonSerializer.Serialize(profile, ProgressJsonContext.Default.LearnerProfile));
        Changed?.Invoke();
    }

    /// <summary>
    /// The reader's level for a page address: the stack's own level if set, otherwise the general one.
    /// Junior until the reader chooses, so nothing above the basics is counted by mistake.
    /// </summary>
    public Level LevelFor(string path)
    {
        if (Profile is null)
            return Level.Junior;

        var stack = StackOf(path);
        return Profile.StackLevels is not null && Profile.StackLevels.TryGetValue(stack, out var level) ? level : Profile.Level;
    }

    /// <summary>Whether something meant for this level is shown on the page. Null means the topic's own level.</summary>
    public bool Shows(Level? level, string path) => level is null || level <= LevelFor(path);

    /// <summary>The stack of a page address: its first part.</summary>
    public static string StackOf(string path)
    {
        var end = path.IndexOf('/', StringComparison.Ordinal);
        return end < 0 ? path : path[..end];
    }
}
