using Basic.Reference.Assemblies;
using Override.Content.Model;
using Override.Content.Parsing;
using Override.Content.Site;
using Override.Runner;

namespace Override.Content.Rendering;

/// <summary>
/// Runs every C# task the way the site will: the reference solution must pass its tests, otherwise the
/// build fails, since a reader could never solve the task. A starter that already passes is reported too:
/// such a task checks nothing.
/// </summary>
internal static class SolutionVerifier
{
    private const string CSharpLanguage = "csharp";

    private static readonly Lazy<CSharpRunner> _runner = new(() => new CSharpRunner(Net100.References.All));

    public static void Verify(IEnumerable<SourceTopic> topics, ICollection<ContentDiagnostic> diagnostics)
    {
        foreach (var topic in topics)
        {
            // Every variant is a task of its own: its solution must pass and its starter must not.
            var tasks = topic.Topic.Steps
                .SelectMany(step => step.Blocks)
                .OfType<QuizBlock>()
                .SelectMany(block => block.AllVersions.Select(quiz => (block.Line, Quiz: quiz)))
                .Where(item => item.Quiz is CodeQuiz { Language: CSharpLanguage });

            foreach (var block in tasks)
            {
                var task = (CodeQuiz)block.Quiz;
                var solution = Run(task.Solution, task.Tests);
                if (!solution.Passed)
                {
                    diagnostics.Add(new(
                        topic.FilePath,
                        block.Line,
                        $"The solution of '{task.Id}' does not pass its tests: {string.Join(" ", solution.Messages)}"));
                }

                if (Run(task.Starter, task.Tests).Passed)
                {
                    diagnostics.Add(new(
                        topic.FilePath,
                        block.Line,
                        $"The starter of '{task.Id}' already passes the tests, so the task checks nothing.",
                        DiagnosticSeverity.Warning));
                }
            }
        }
    }

    /// <summary>The builder is a console program without a synchronization context, so waiting here is safe.</summary>
    private static RunResult Run(string code, string tests) =>
        _runner.Value.RunTestsAsync(code, tests).GetAwaiter().GetResult();
}
