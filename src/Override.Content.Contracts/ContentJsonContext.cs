using System.Text.Json.Serialization;

namespace Override.Content.Model;

/// <summary>Source-generated JSON metadata for the compiled content, so the site needs no reflection.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SiteIndex))]
[JsonSerializable(typeof(TopicPage))]
public sealed partial class ContentJsonContext : JsonSerializerContext
{
}
