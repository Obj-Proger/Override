using Basic.Reference.Assemblies;
using Override.Runner;

namespace Override.Web.Running;

/// <summary>
/// The only place that touches the compiler's types. Its methods run after the lazy assemblies are loaded;
/// the runner is passed around as object, so no other type depends on those assemblies.
/// </summary>
internal static class RunnerBridge
{
    public static object Create() => new CSharpRunner(Net100.References.All);

    public static async Task<CodeRunResult> RunTestsAsync(object runner, string code, string tests)
    {
        var result = await ((CSharpRunner)runner).RunTestsAsync(code, tests);
        var status = result.Status switch
        {
            RunStatus.CompileError => CodeRunStatus.CompileError,
            RunStatus.Passed => CodeRunStatus.Passed,
            RunStatus.Failed => CodeRunStatus.Failed,
            RunStatus.Stopped => CodeRunStatus.Stopped,
            _ => CodeRunStatus.Crashed,
        };

        return new CodeRunResult(status, result.Messages, result.Output);
    }
}
