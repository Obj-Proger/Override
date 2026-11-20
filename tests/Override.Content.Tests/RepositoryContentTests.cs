using Override.Content.Parsing;

namespace Override.Content.Tests;

/// <summary>Every topic in the repository must parse: a broken article fails the build.</summary>
public sealed class RepositoryContentTests
{
    [Fact]
    public void AllTopicsParse()
    {
        var contentDirectory = Path.Combine(FindRepositoryRoot(), "content");

        // Files directly in content/ (FORMAT.md, LICENSE.md) describe the content and are not topics.
        var topics = Directory
            .EnumerateFiles(contentDirectory, "*.md", SearchOption.AllDirectories)
            .Where(path => Path.GetDirectoryName(path) != contentDirectory)
            .ToList();

        Assert.NotEmpty(topics);

        var diagnostics = topics
            .SelectMany(path => TopicParser.Parse(Path.GetRelativePath(contentDirectory, path), File.ReadAllText(path)).Diagnostics)
            .ToList();

        Assert.Empty(diagnostics);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Override.slnx")))
                return directory.FullName;
        }

        throw new InvalidOperationException("Override.slnx was not found above the test output directory.");
    }
}
