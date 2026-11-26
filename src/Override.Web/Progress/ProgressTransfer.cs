using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Override.Web.Progress;

/// <summary>What a transfer link holds, to show before importing it.</summary>
public sealed record TransferSummary(int Answers, int Topics, int Tests, DateTimeOffset? LastAnswer);

/// <summary>
/// Moves progress between devices without a server: everything is packed into a link, the person opens it on the
/// other device and the site offers to import it. The data travels after #, so no server ever receives it.
/// </summary>
public sealed class ProgressTransfer(ProgressStore progress, ProfileStore profile, IJSRuntime js, NavigationManager navigation)
{
    public const int CurrentVersion = 1;

    private const string Marker = "#import=";

    public async Task<string> CreateLinkAsync()
    {
        var data = new TransferData(CurrentVersion, progress.Data, profile.Profile);
        var json = JsonSerializer.Serialize(data, ProgressJsonContext.Default.TransferData);
        var code = await js.InvokeAsync<string>("overrideTransfer.pack", json);
        return navigation.BaseUri + Marker + code;
    }

    /// <summary>The code of a link opened on this device, once: it is removed from the address.</summary>
    public string? TakeFromAddress() => ((IJSInProcessRuntime)js).Invoke<string?>("overrideTransfer.takeFromAddress");

    /// <summary>Reads a whole link or just its code; null if it is damaged or not a transfer link.</summary>
    public async Task<TransferData?> ReadAsync(string linkOrCode)
    {
        var text = linkOrCode.Trim();
        var at = text.IndexOf(Marker, StringComparison.Ordinal);
        var code = at >= 0 ? text[(at + Marker.Length)..] : text;
        if (code.Length == 0)
            return null;

        try
        {
            var json = await js.InvokeAsync<string>("overrideTransfer.unpack", code);
            var data = JsonSerializer.Deserialize(json, ProgressJsonContext.Default.TransferData);
            return data is { Version: <= CurrentVersion, Progress: not null } ? data : null;
        }
        catch (Exception exception) when (exception is JSException or JsonException)
        {
            return null;
        }
    }

    public static TransferSummary Summarize(TransferData data) => new(
        data.Progress.Questions.Values.Sum(attempts => attempts.Count),
        data.Progress.Topics.Count,
        data.Progress.Tests.Values.Sum(results => results.Count),
        data.Progress.Questions.Values.SelectMany(attempts => attempts).Select(attempt => (DateTimeOffset?)attempt.At).Max());

    /// <summary>Imports the progress; the level and direction come along when replacing or when none is chosen here.</summary>
    public void Apply(TransferData data, bool replace)
    {
        progress.Import(data.Progress, replace);
        if (data.Profile is { } chosen && (replace || profile.Profile is null))
            profile.Set(chosen);
    }

    public bool CanShare() => ((IJSInProcessRuntime)js).Invoke<bool>("overrideTransfer.canShare");

    public async Task<bool> ShareAsync(string link) => await js.InvokeAsync<bool>("overrideTransfer.share", link);
}
