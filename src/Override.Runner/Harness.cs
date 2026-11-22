using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Override.Runner;

/// <summary>Source code compiled together with the reader's code: usings, limits, checks and the test method.</summary>
internal static class Harness
{
    public const string TestsType = "__OverrideTests";
    public const string GuardType = "__OverrideGuard";
    public const string AssertException = "__OverrideAssertException";
    public const string LimitException = "__OverrideLimitException";

    /// <summary>The same namespaces as in the highlighted snippets, so tasks are written like the examples.</summary>
    public const string GlobalUsings = """
        global using System;
        global using System.Collections;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Text;
        global using System.Threading;
        global using System.Threading.Tasks;
        """;

    /// <summary>
    /// The guard counts loop iterations and recursion depth and stops the run past the limits. Once stopped,
    /// it keeps throwing, so catching its exception in the reader's code does not let a loop go on.
    /// Assert has the checks the tests use.
    /// </summary>
    public const string Support = """
        public static class __OverrideGuard
        {
            private static long _maxIterations;
            private static long _iterations;
            private static long _deadline;
            private static int _maxDepth;
            private static int _depth;

            public static bool Stopped { get; private set; }

            public static void Reset(long maxIterations, int maxDepth, long deadline)
            {
                _maxIterations = maxIterations;
                _maxDepth = maxDepth;
                _deadline = deadline;
                _iterations = 0;
                _depth = 0;
                Stopped = false;
            }

            public static void Tick()
            {
                if (Stopped || ++_iterations > _maxIterations || System.Diagnostics.Stopwatch.GetTimestamp() > _deadline)
                    Stop("Код выполнялся слишком долго: похоже на бесконечный цикл.");
            }

            public static void Enter()
            {
                if (Stopped)
                    Stop("Код выполнялся слишком долго.");

                if (++_depth > _maxDepth)
                    Stop("Слишком глубокая рекурсия: похоже, метод вызывает себя без условия выхода.");
            }

            public static void Exit() => _depth--;

            private static void Stop(string message)
            {
                Stopped = true;
                throw new __OverrideLimitException(message);
            }
        }

        public sealed class __OverrideLimitException(string message) : System.Exception(message);

        public sealed class __OverrideAssertException(string message) : System.Exception(message);

        public static class Assert
        {
            public static void Equal<T>(T expected, T actual)
            {
                if (!Same(expected, actual))
                    throw new __OverrideAssertException($"ожидалось {Show(expected)}, получено {Show(actual)}");
            }

            public static void NotEqual<T>(T expected, T actual)
            {
                if (Same(expected, actual))
                    throw new __OverrideAssertException($"ожидалось значение, отличное от {Show(expected)}");
            }

            public static void True(bool condition)
            {
                if (!condition)
                    throw new __OverrideAssertException("ожидалось true, получено false");
            }

            public static void False(bool condition)
            {
                if (condition)
                    throw new __OverrideAssertException("ожидалось false, получено true");
            }

            public static void Null(object? value)
            {
                if (value is not null)
                    throw new __OverrideAssertException($"ожидалось null, получено {Show(value)}");
            }

            public static void NotNull(object? value)
            {
                if (value is null)
                    throw new __OverrideAssertException("ожидалось значение, получено null");
            }

            public static TException Throws<TException>(System.Action action)
                where TException : System.Exception
            {
                try
                {
                    action();
                }
                catch (TException exception)
                {
                    return exception;
                }
                catch (System.Exception exception) when (exception is not __OverrideLimitException)
                {
                    throw new __OverrideAssertException(
                        $"ожидалось исключение {typeof(TException).Name}, получено {exception.GetType().Name}");
                }

                throw new __OverrideAssertException($"ожидалось исключение {typeof(TException).Name}, но его не было");
            }

            private static bool Same<T>(T expected, T actual)
            {
                if (expected is IEnumerable left && actual is IEnumerable right && expected is not string)
                    return left.Cast<object?>().SequenceEqual(right.Cast<object?>());

                return EqualityComparer<T>.Default.Equals(expected, actual);
            }

            private static string Show(object? value) => value switch
            {
                null => "null",
                string text => $"\"{text}\"",
                IEnumerable items => "[" + string.Join(", ", items.Cast<object?>().Select(Show)) + "]",
                _ => value.ToString() ?? "",
            };
        }
        """;

    /// <summary>
    /// The test method. Before each check it records the check's text, so a failure or an exception can say
    /// which check it was.
    /// </summary>
    public static string Tests(string tests)
    {
        var block = (BlockSyntax)SyntaxFactory.ParseStatement("{\n" + tests + "\n}");
        var source = new StringBuilder()
            .Append("public static class ").AppendLine(TestsType)
            .AppendLine("{")
            .AppendLine("    public static string Current = \"\";")
            .AppendLine()
            .AppendLine("    public static async System.Threading.Tasks.Task Run()")
            .AppendLine("    {");

        foreach (var statement in block.Statements)
        {
            var text = statement.ToString().Trim();
            source.Append("        Current = @\"").Append(text.Replace("\"", "\"\"", StringComparison.Ordinal)).AppendLine("\";");
            source.Append("        ").AppendLine(text);
        }

        return source
            .AppendLine("        await System.Threading.Tasks.Task.CompletedTask;")
            .AppendLine("    }")
            .AppendLine("}")
            .ToString();
    }
}
