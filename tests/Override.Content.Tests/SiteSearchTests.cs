using Override.Content.Search;

namespace Override.Content.Tests;

public sealed class SiteSearchTests
{
    private static readonly SearchIndex _index = new(
    [
        new SearchEntry("a/boxing", "Boxing и unboxing", "C# и .NET › Система типов", "При boxing значение копируется в обёртку в куче."),
        new SearchEntry("a/strings", "Строки", "C# и .NET › Строки", "Строка неизменяема; boxing здесь не нужен."),
        new SearchEntry("a/gc", "Сборка мусора", "C# и .NET › Память", "Куча делится на поколения."),
    ]);

    [Fact]
    public void TitleMatchesComeFirst()
    {
        var hits = SiteSearch.Find(_index, "boxing");

        Assert.Equal(new[] { "a/boxing", "a/strings" }, hits.Select(h => h.Entry.Path));
    }

    [Fact]
    public void EveryWordMustBeFound()
    {
        Assert.Equal("a/boxing", Assert.Single(SiteSearch.Find(_index, "boxing куче")).Entry.Path);
        Assert.Empty(SiteSearch.Find(_index, "boxing поколения"));
    }

    [Fact]
    public void IgnoresCaseAndYo() =>
        Assert.Equal("a/boxing", Assert.Single(SiteSearch.Find(_index, "ОБЕРТКУ")).Entry.Path);

    [Fact]
    public void SnippetShowsTheTextAroundTheMatch()
    {
        var hit = Assert.Single(SiteSearch.Find(_index, "поколения"));

        Assert.Contains("поколения", hit.Snippet, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyQueryFindsNothing() => Assert.Empty(SiteSearch.Find(_index, "   "));
}
