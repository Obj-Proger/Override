using System.Net.Http.Json;
using Override.Content.Model;
using Override.Content.Search;

namespace Override.Web.Content;

/// <summary>Loads the compiled content that the build puts under wwwroot/content.</summary>
public sealed class ContentClient(HttpClient http)
{
    private readonly Dictionary<string, Task<TopicPage>> _topics = [];
    private Task<SiteIndex>? _index;
    private Task<SearchIndex>? _search;

    /// <summary>The table of contents. Loaded once per visit.</summary>
    public Task<SiteIndex> GetIndexAsync() => _index ??= LoadIndexAsync();

    /// <summary>The search index, loaded on the first search.</summary>
    public Task<SearchIndex> GetSearchAsync() => _search ??= LoadSearchAsync();

    /// <summary>A compiled topic. Each topic is loaded once per visit.</summary>
    public Task<TopicPage> GetTopicAsync(string id)
    {
        if (!_topics.TryGetValue(id, out var topic))
            _topics[id] = topic = LoadTopicAsync(id);

        return topic;
    }

    private async Task<TopicPage> LoadTopicAsync(string id) =>
        await http.GetFromJsonAsync($"content/topics/{id}.json", ContentJsonContext.Default.TopicPage)
        ?? throw new InvalidOperationException($"Topic '{id}' is empty.");

    private async Task<SearchIndex> LoadSearchAsync() =>
        await http.GetFromJsonAsync("content/search.json", ContentJsonContext.Default.SearchIndex)
        ?? throw new InvalidOperationException("The search index is empty.");

    private async Task<SiteIndex> LoadIndexAsync() =>
        await http.GetFromJsonAsync("content/index.json", ContentJsonContext.Default.SiteIndex)
        ?? throw new InvalidOperationException("The content index is empty.");
}
