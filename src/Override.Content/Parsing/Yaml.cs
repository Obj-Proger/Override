using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Override.Content.Parsing;

/// <summary>Shared YAML reading. Unknown keys are errors, so a typo in a field name is caught.</summary>
internal static class Yaml
{
    private static readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    /// <summary>
    /// Reads <paramref name="yaml"/> into <typeparamref name="T"/>. On failure adds a diagnostic whose line
    /// is counted from <paramref name="firstLine"/>, the file line where the YAML starts.
    /// </summary>
    public static T? Read<T>(string yaml, string file, int firstLine, ICollection<ContentDiagnostic> diagnostics)
        where T : class
    {
        try
        {
            var document = _deserializer.Deserialize<T>(yaml);
            if (document is null)
                diagnostics.Add(new(file, firstLine, "The YAML block is empty."));

            return document;
        }
        catch (YamlException ex)
        {
            var line = firstLine + Math.Max(0, (int)ex.Start.Line - 1);
            var message = ex.InnerException?.Message ?? ex.Message;
            diagnostics.Add(new(file, line, $"Invalid YAML: {message}"));
            return null;
        }
    }
}
