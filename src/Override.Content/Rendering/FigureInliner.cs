using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Override.Content.Parsing;

namespace Override.Content.Rendering;

/// <summary>
/// Replaces a line with only an image of a local SVG file, ![description](file.svg "caption"),
/// with the SVG itself inside a figure. Inline SVG follows the site theme through CSS variables.
/// </summary>
/// <remarks>
/// Ids and styles of an inline SVG belong to the whole page, so two figures with the same class
/// or id would affect each other. Every figure is scoped: ids get the figure name as a prefix,
/// and style rules apply only inside the figure's root element.
/// </remarks>
internal static partial class FigureInliner
{
    [GeneratedRegex("^!\\[(?<alt>[^\\]]*)\\]\\((?<file>[a-z0-9]+(-[a-z0-9]+)*\\.svg)(\\s+\"(?<caption>[^\"]*)\")?\\)\\s*$")]
    private static partial Regex FigureLine();

    [GeneratedRegex("<svg\\b(?<attributes>[^>]*)>")]
    private static partial Regex RootElement();

    [GeneratedRegex("\\s(class|id)\\s*=")]
    private static partial Regex ClassOrId();

    [GeneratedRegex("(?<attribute>\\sid\\s*=\\s*\")(?<id>[^\"]+)\"")]
    private static partial Regex IdAttribute();

    [GeneratedRegex("url\\(\\s*#(?<id>[^)\\s]+)\\s*\\)")]
    private static partial Regex UrlReference();

    [GeneratedRegex("(?<attribute>\\s(xlink:)?href\\s*=\\s*\"#)(?<id>[^\"]+)\"")]
    private static partial Regex HrefReference();

    [GeneratedRegex("(?<attribute>\\saria-(labelledby|describedby)\\s*=\\s*\")(?<ids>[^\"]+)\"")]
    private static partial Regex AriaReference();

    [GeneratedRegex("(?<open><style[^>]*>)(?<css>.*?)(?<close></style>)", RegexOptions.Singleline)]
    private static partial Regex StyleElement();

    [GeneratedRegex("(?<selectors>[^{}]+)\\{(?<body>[^{}]*)\\}")]
    private static partial Regex StyleRule();

    public static string Inline(
        string markdown, string topicFile, int firstLine, ICollection<ContentDiagnostic> diagnostics)
    {
        var lines = markdown.Split('\n');
        var directory = Path.GetDirectoryName(topicFile) ?? "";
        var result = new StringBuilder();

        for (var i = 0; i < lines.Length; i++)
        {
            var match = FigureLine().Match(lines[i]);
            if (!match.Success)
            {
                result.Append(lines[i]).Append('\n');
                continue;
            }

            var file = match.Groups["file"].Value;
            var line = firstLine + i;
            void Error(string message) => diagnostics.Add(new(topicFile, line, message));

            var path = Path.Combine(directory, file);
            if (!File.Exists(path))
            {
                Error($"Figure '{file}' is not found next to the topic.");
                continue;
            }

            var svg = Scope(File.ReadAllText(path), Path.GetFileNameWithoutExtension(file), Error);
            if (svg is not null)
                result.Append(Figure(svg, match.Groups["alt"].Value, match.Groups["caption"].Value)).Append('\n');
        }

        return result.ToString().TrimEnd('\n');
    }

    /// <summary>Returns the SVG with its ids and styles made local to the figure, or null after reporting a problem.</summary>
    private static string? Scope(string svg, string name, Action<string> error)
    {
        var start = svg.IndexOf("<svg", StringComparison.Ordinal);
        var root = start < 0 ? Match.Empty : RootElement().Match(svg, start);
        if (!root.Success)
        {
            error($"'{name}.svg' is not an SVG image.");
            return null;
        }

        if (ClassOrId().IsMatch(root.Groups["attributes"].Value))
        {
            error($"The root <svg> of '{name}.svg' must not have a class or an id: the builder sets them.");
            return null;
        }

        if (!svg.Contains("<title", StringComparison.Ordinal) || !svg.Contains("<desc", StringComparison.Ordinal))
        {
            error($"'{name}.svg' needs <title> and <desc> that describe the figure in words.");
            return null;
        }

        var scope = $"fig-{name}";
        var css = StyleElement().Matches(svg).Select(m => m.Groups["css"].Value);
        if (css.Any(c => c.Contains('@', StringComparison.Ordinal)))
        {
            error($"'{name}.svg' uses an at-rule in <style>: only plain rules can be scoped to the figure.");
            return null;
        }

        var body = svg[start..];
        body = RootElement().Replace(body, m => $"<svg class=\"{scope}\"{m.Groups["attributes"].Value}>", 1);
        body = IdAttribute().Replace(body, m => $"{m.Groups["attribute"].Value}{scope}-{m.Groups["id"].Value}\"");
        body = UrlReference().Replace(body, m => $"url(#{scope}-{m.Groups["id"].Value})");
        body = HrefReference().Replace(body, m => $"{m.Groups["attribute"].Value}{scope}-{m.Groups["id"].Value}\"");
        body = AriaReference().Replace(body, m =>
        {
            var ids = m.Groups["ids"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return $"{m.Groups["attribute"].Value}{string.Join(' ', ids.Select(id => $"{scope}-{id}"))}\"";
        });
        body = StyleElement().Replace(body, m =>
            m.Groups["open"].Value + ScopeCss(m.Groups["css"].Value, scope) + m.Groups["close"].Value);

        return body;
    }

    private static string ScopeCss(string css, string scope) => StyleRule().Replace(css, rule =>
    {
        var selectors = rule.Groups["selectors"].Value;
        var indent = selectors[..^selectors.TrimStart().Length];
        var scoped = selectors.Trim()
            .Split(',')
            .Select(selector => $".{scope} {selector.Trim()}");

        return $"{indent}{string.Join(',', scoped)}{{{rule.Groups["body"].Value}}}";
    });

    /// <summary>
    /// An HTML block in Markdown ends at an empty line, so empty lines inside the SVG are dropped
    /// and the figure is written as one continuous block.
    /// </summary>
    private static string Figure(string svg, string description, string caption)
    {
        var body = string.Join(
            '\n',
            svg.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.TrimEnd()));

        var figure = new StringBuilder("<figure class=\"figure\"");
        if (description.Length > 0)
            figure.Append(" aria-label=\"").Append(WebUtility.HtmlEncode(description)).Append('"');

        figure.Append(">\n").Append(body).Append('\n');
        if (caption.Length > 0)
            figure.Append("<figcaption>").Append(WebUtility.HtmlEncode(caption)).Append("</figcaption>\n");

        return figure.Append("</figure>").ToString();
    }
}
