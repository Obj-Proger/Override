using Override.Content.Parsing;
using Override.Content.Rendering;

namespace Override.Content.Tests;

/// <summary>The content in the repository compiles: a broken article fails the tests as well as the site build.</summary>
public sealed class RepositoryContentTests
{
    [Fact]
    public void EveryLanguageCompiles()
    {
        // Each folder in content/ is a language. Files there (FORMAT.md, LICENSE.md) describe the content.
        var languages = Directory.GetDirectories(Path.Combine(FindRepositoryRoot(), "content"));
        Assert.NotEmpty(languages);

        foreach (var language in languages)
        {
            var site = SiteCompiler.Compile(language);

            Assert.NotEmpty(site.Pages);
            Assert.DoesNotContain(site.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        }
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
