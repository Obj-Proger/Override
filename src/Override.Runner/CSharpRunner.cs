using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Override.Runner;

/// <summary>
/// Compiles the reader's code with the task's tests and runs them. The code is first compiled as written,
/// so errors point at the reader's lines; then compiled again with guard calls and run under the limits.
/// </summary>
public sealed class CSharpRunner(IEnumerable<MetadataReference> references)
{
    private const string SolutionFile = "Solution.cs";
    private const string TestsFile = "Tests.cs";

    private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Latest);

    /// <summary>
    /// One thread, as in the browser, where WebAssembly has no threads to compile on.
    /// Nullable annotations are allowed; warnings never stop a run.
    /// </summary>
    private static readonly CSharpCompilationOptions _compilationOptions = new(
        OutputKind.DynamicallyLinkedLibrary,
        nullableContextOptions: NullableContextOptions.Enable,
        concurrentBuild: false);

    private readonly ImmutableArray<MetadataReference> _references = [.. references];

    public RunLimits Limits { get; init; } = RunLimits.Default;

    public async Task<RunResult> RunTestsAsync(string code, string tests)
    {
        var usings = CSharpSyntaxTree.ParseText(Harness.GlobalUsings, _parseOptions);
        var support = CSharpSyntaxTree.ParseText(Harness.Support, _parseOptions);
        var solution = CSharpSyntaxTree.ParseText(code, _parseOptions, path: SolutionFile);
        var checks = CSharpSyntaxTree.ParseText(Harness.Tests(tests), _parseOptions, path: TestsFile);

        var errors = Compile([usings, support, solution, checks])
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        if (errors.Count > 0)
            return new RunResult(RunStatus.CompileError, errors.Select(Describe).Distinct().ToList(), "");

        var guarded = Compile([usings, support, GuardRewriter.Guard(solution), GuardRewriter.Guard(checks)]);
        using var image = new MemoryStream();
        var emitted = guarded.Emit(image);
        if (!emitted.Success)
        {
            var messages = emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(Describe).ToList();
            return new RunResult(RunStatus.CompileError, messages, "");
        }

        return await ExecuteAsync(Assembly.Load(image.ToArray()));
    }

    private CSharpCompilation Compile(IEnumerable<SyntaxTree> trees) =>
        CSharpCompilation.Create("Solution", trees, _references, _compilationOptions);

    /// <summary>Errors in the reader's code carry its line; errors in the tests mean the code does not fit them.</summary>
    private static string Describe(Diagnostic diagnostic)
    {
        var span = diagnostic.Location.GetLineSpan();
        var message = diagnostic.GetMessage();
        return span.Path switch
        {
            SolutionFile => $"Строка {span.StartLinePosition.Line + 1}: {message}",
            TestsFile => $"Проверки не компилируются с этим кодом: {message}",
            _ => message,
        };
    }

    private async Task<RunResult> ExecuteAsync(Assembly assembly)
    {
        var guard = assembly.GetType(Harness.GuardType)!;
        var tests = assembly.GetType(Harness.TestsType)!;
        var deadline = Stopwatch.GetTimestamp() + (long)(Limits.Time.TotalSeconds * Stopwatch.Frequency);
        guard.GetMethod("Reset")!.Invoke(null, [Limits.MaxIterations, Limits.MaxDepth, deadline]);

        var output = new StringWriter();
        var console = Console.Out;
        Console.SetOut(output);

        try
        {
            var run = (Task)tests.GetMethod("Run")!.Invoke(null, null)!;

            // A run that waits on something real is bounded by time, not by the guard.
            var finished = await Task.WhenAny(run, Task.Delay(Limits.Time));
            if (finished != run)
                return Result(RunStatus.Stopped, "Код выполнялся слишком долго.");

            await run;
            return (bool)guard.GetProperty("Stopped")!.GetValue(null)!
                ? Result(RunStatus.Stopped, "Код выполнялся слишком долго: похоже на бесконечный цикл.")
                : Result(RunStatus.Passed);
        }
        catch (Exception exception)
        {
            var inner = exception is TargetInvocationException { InnerException: { } wrapped } ? wrapped : exception;
            var check = (string?)tests.GetField("Current")!.GetValue(null);
            return inner.GetType().Name switch
            {
                Harness.AssertException => Result(RunStatus.Failed, $"Проверка {check}: {inner.Message}."),
                Harness.LimitException => Result(RunStatus.Stopped, inner.Message),
                _ => Result(RunStatus.Crashed, $"Проверка {check}: исключение {inner.GetType().Name}: {inner.Message}"),
            };
        }
        finally
        {
            Console.SetOut(console);
        }

        RunResult Result(RunStatus status, params string[] messages)
        {
            var text = output.ToString();
            if (text.Length > Limits.MaxOutput)
                text = text[..Limits.MaxOutput] + "\n…";

            return new RunResult(status, messages, text);
        }
    }
}
