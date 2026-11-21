using System.Text.RegularExpressions;

namespace Override.Content.Highlighting;

/// <summary>Colors SQL by tokens: keywords and types, strings, numbers and comments.</summary>
internal static partial class SqlHighlighter
{
    private static readonly HashSet<string> _keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Statements and clauses
        "select", "from", "where", "and", "or", "not", "in", "is", "null", "as", "on", "join", "inner", "left",
        "right", "full", "outer", "cross", "lateral", "group", "by", "order", "having", "limit", "offset", "fetch",
        "first", "next", "rows", "only", "distinct", "all", "any", "some", "exists", "between", "like", "ilike",
        "case", "when", "then", "else", "end", "union", "intersect", "except", "with", "recursive", "insert",
        "into", "values", "update", "set", "delete", "returning", "create", "alter", "drop", "table", "index",
        "unique", "primary", "foreign", "key", "references", "constraint", "check", "default", "if", "cascade",
        "restrict", "view", "materialized", "sequence", "schema", "database", "concurrently", "using", "begin",
        "commit", "rollback", "savepoint", "transaction", "isolation", "level", "read", "committed", "repeatable",
        "serializable", "for", "share", "nowait", "skip", "locked", "asc", "desc", "nulls", "last", "over",
        "partition", "window", "filter", "true", "false", "explain", "analyze", "vacuum", "grant", "revoke",
        "conflict", "do", "nothing", "add", "column", "rename", "to", "type", "trigger", "function", "returns",
        "language", "replace", "include", "generated", "always", "identity", "stored", "collate",

        // Types
        "int", "integer", "smallint", "bigint", "serial", "bigserial", "numeric", "decimal", "real", "double",
        "precision", "boolean", "bool", "text", "varchar", "char", "character", "varying", "uuid", "date",
        "time", "timestamp", "timestamptz", "interval", "zone", "json", "jsonb", "bytea", "money",
    };

    [GeneratedRegex(
        "(?<cmt>--[^\\n]*|/\\*.*?\\*/)|(?<str>'(?:[^']|'')*')|(?<num>\\b\\d+(?:\\.\\d+)?\\b)|(?<word>\\b[A-Za-z_][A-Za-z0-9_]*\\b)",
        RegexOptions.Singleline)]
    private static partial Regex Token();

    public static string Highlight(string code)
    {
        var classes = new string?[code.Length];

        foreach (Match match in Token().Matches(code))
        {
            var css = match.Groups["cmt"].Success ? "cmt"
                : match.Groups["str"].Success ? "str"
                : match.Groups["num"].Success ? "num"
                : _keywords.Contains(match.Value) ? "kw"
                : null;

            for (var i = match.Index; i < match.Index + match.Length; i++)
                classes[i] = css;
        }

        return TokenRuns.ToHtml(code, classes);
    }
}
