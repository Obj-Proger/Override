using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Override.Web.Running;

/// <summary>How a code run ended, in the site's own terms.</summary>
public enum CodeRunStatus
{
    CompileError,
    Passed,
    Failed,
    Crashed,
    Stopped,
}

/// <summary>The result of a code run: its status, messages for the reader and the printed output.</summary>
public sealed record CodeRunResult(CodeRunStatus Status, IReadOnlyList<string> Messages, string Output);

/// <summary>
/// Runs C# tasks in the browser. The compiler is loaded on the first run: until then the site does not
/// download it. Types of the compiler are used only inside <see cref="RunnerBridge"/>, after loading.
/// </summary>
public sealed class CodeRunService(LazyAssemblyLoader loader)
{
    private static readonly string[] _assemblies =
    [
        "Microsoft.CodeAnalysis.wasm",
        "Microsoft.CodeAnalysis.CSharp.wasm",
        "Basic.Reference.Assemblies.Net100.wasm",
        "Override.Runner.wasm",
    ];

    private Task<object>? _runner;

    /// <summary>Whether the compiler is already loaded, so the first run will not wait for the download.</summary>
    public bool IsLoaded => _runner is { IsCompletedSuccessfully: true };

    public async Task<CodeRunResult> RunTestsAsync(string code, string tests)
    {
        var runner = await (_runner ??= LoadAsync());
        return await RunnerBridge.RunTestsAsync(runner, code, tests);
    }

    private async Task<object> LoadAsync()
    {
        try
        {
            await loader.LoadAssembliesAsync(_assemblies);
            return RunnerBridge.Create();
        }
        catch
        {
            // A failed download can be retried with the next run.
            _runner = null;
            throw;
        }
    }
}
