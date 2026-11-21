using System.Text.Json;
using Microsoft.JSInterop;
using Override.Content.Model;

namespace Override.Web.Progress;

/// <summary>The level and direction the reader chose. They decide which blocks and questions are shown.</summary>
public sealed record LearnerProfile(Level Level, Track Track);

/// <summary>Keeps the reader's choice in local storage. Until the reader chooses, Profile is null.</summary>
public sealed class ProfileStore(IJSRuntime js)
{
    private const string StorageKey = "override.profile";

    public LearnerProfile? Profile { get; private set; }

    /// <summary>The reader's level; Junior until chosen, so nothing above the basics is counted by mistake.</summary>
    public Level Level => Profile?.Level ?? Level.Junior;

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
        catch (JsonException)
        {
            // Damaged data: the reader is asked again.
        }
    }

    public void Set(LearnerProfile profile)
    {
        Profile = profile;
        ((IJSInProcessRuntime)js).Invoke<bool>(
            "overrideStorage.set", StorageKey, JsonSerializer.Serialize(profile, ProgressJsonContext.Default.LearnerProfile));
        Changed?.Invoke();
    }

    /// <summary>Whether something meant for this level is shown to the reader. Null means the topic's own level.</summary>
    public bool Shows(Level? level) => level is null || level <= Level;
}
