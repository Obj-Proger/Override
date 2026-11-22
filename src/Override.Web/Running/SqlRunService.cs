using System.Text.Json;
using Microsoft.JSInterop;
using Override.Content.Checking;
using Override.Content.Model;

namespace Override.Web.Running;

/// <summary>How a SQL run ended: the query result, and either what differs from the reference or an error.</summary>
public sealed record SqlRunResult(bool Executed, bool Matches, string? Message, SqlTable? Table);

/// <summary>
/// Runs SQL tasks in the browser on PGlite. PostgreSQL is loaded on the first run; the reference result of each
/// task is computed once and kept for the visit.
/// </summary>
public sealed class SqlRunService(IJSRuntime js)
{
    private const string ModulePath = "./lib/pglite/sql.js";

    private readonly Dictionary<string, SqlTable> _expected = [];
    private Task<IJSObjectReference>? _module;

    public bool IsLoaded => _module is { IsCompletedSuccessfully: true };

    public async Task<SqlRunResult> RunAsync(SqlQuiz task, string query)
    {
        var module = await LoadAsync();

        var (ran, error, actual) = await QueryAsync(module, task.Setup, query);
        if (!ran)
            return new SqlRunResult(Executed: false, Matches: false, error, null);

        var key = task.Setup + "\0" + task.Solution;
        if (!_expected.TryGetValue(key, out var expected))
        {
            var (solved, solutionError, table) = await QueryAsync(module, task.Setup, task.Solution);
            if (!solved)
                return new SqlRunResult(Executed: false, Matches: false, $"Эталонный запрос не выполнился: {solutionError}", actual);

            _expected[key] = expected = table!;
        }

        var difference = SqlComparer.Difference(expected, actual!, task.Ordered);
        return new SqlRunResult(Executed: true, Matches: difference is null, difference, actual);
    }

    private Task<IJSObjectReference> LoadAsync()
    {
        _module ??= js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
        if (_module.IsFaulted)
            _module = js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();

        return _module;
    }

    /// <summary>The result is read by hand from JSON: no reflection, so trimming cannot break it.</summary>
    private static async Task<(bool Ran, string? Error, SqlTable? Table)> QueryAsync(IJSObjectReference module, string setup, string query)
    {
        var result = await module.InvokeAsync<JsonElement>("run", setup, query);
        if (!result.GetProperty("ok").GetBoolean())
            return (false, result.GetProperty("error").GetString(), null);

        var columns = result.GetProperty("columns").EnumerateArray().Select(c => c.GetString() ?? "").ToList();
        var rows = result.GetProperty("rows")
            .EnumerateArray()
            .Select(row => (IReadOnlyList<string>)row.EnumerateArray().Select(v => v.GetString() ?? "").ToList())
            .ToList();

        return (true, null, new SqlTable(columns, rows));
    }
}
