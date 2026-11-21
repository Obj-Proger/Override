namespace Override.Content.Tests;

/// <summary>A content folder in the temp directory, deleted after the test.</summary>
internal sealed class TempContent : IDisposable
{
    public TempContent() => Directory.CreateDirectory(Root);

    public string Root { get; } = Path.Combine(Path.GetTempPath(), "override-tests", Guid.NewGuid().ToString("N"));

    /// <summary>Creates a section folder with its _section.yml.</summary>
    public TempContent Section(string path, string title, int order) =>
        Write($"{path}/_section.yml", $"title: {title}\norder: {order}\n");

    public TempContent Write(string path, string text)
    {
        var fullPath = Path.Combine(Root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, text);
        return this;
    }

    public string PathOf(string path) => Path.Combine(Root, path);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp folder does not affect other tests: each one uses a new folder.
        }
    }
}
