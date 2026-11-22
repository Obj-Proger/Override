namespace Override.Content.Search;

/// <summary>A topic for the site search: its address, title, section path and plain text.</summary>
public sealed record SearchEntry(string Path, string Title, string Section, string Text);

/// <summary>All topics of one language for the site search.</summary>
public sealed record SearchIndex(IReadOnlyList<SearchEntry> Entries);

/// <summary>A found topic with a piece of text around the first match, or an empty snippet for a title match.</summary>
public sealed record SearchHit(SearchEntry Entry, string Snippet);
