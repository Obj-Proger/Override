using Basic.Reference.Assemblies;

namespace Override.Runner.Tests;

/// <summary>The runner redirects Console, so these tests must not run in parallel with each other.</summary>
public sealed class CSharpRunnerTests
{
    private const string SumTests = """
        Assert.Equal(6L, Calc.Sum(new ArrayList { 1, 2, 3 }));
        Assert.Equal(0L, Calc.Sum(new ArrayList()));
        """;

    private static readonly CSharpRunner _runner = new(Net100.References.All);

    [Fact]
    public async Task PassesARightSolution()
    {
        var result = await _runner.RunTestsAsync(
            """
            public static class Calc
            {
                public static long Sum(ArrayList numbers)
                {
                    long sum = 0;
                    foreach (object item in numbers)
                        sum += (int)item;
                    return sum;
                }
            }
            """,
            SumTests);

        Assert.Equal(RunStatus.Passed, result.Status);
    }

    [Fact]
    public async Task ReportsTheFailedCheck()
    {
        var result = await _runner.RunTestsAsync(
            "public static class Calc { public static long Sum(ArrayList numbers) => 0; }",
            SumTests);

        Assert.Equal(RunStatus.Failed, result.Status);
        var message = Assert.Single(result.Messages);
        Assert.Contains("Calc.Sum(new ArrayList { 1, 2, 3 })", message, StringComparison.Ordinal);
        Assert.Contains("ожидалось 6, получено 0", message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsCompileErrorsWithTheReadersLine()
    {
        var result = await _runner.RunTestsAsync(
            "public static class Calc\n{\n    public static long Sum(ArrayList numbers) => undefined;\n}",
            SumTests);

        Assert.Equal(RunStatus.CompileError, result.Status);
        Assert.Contains(result.Messages, m => m.StartsWith("Строка 3:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReportsCodeThatDoesNotFitTheTests()
    {
        var result = await _runner.RunTestsAsync("public static class Other { }", SumTests);

        Assert.Equal(RunStatus.CompileError, result.Status);
        Assert.Contains(result.Messages, m => m.StartsWith("Проверки не компилируются", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StopsAnEndlessLoop()
    {
        var result = await _runner.RunTestsAsync(
            "public static class Calc { public static long Sum(ArrayList numbers) { while (true) { } } }",
            SumTests);

        Assert.Equal(RunStatus.Stopped, result.Status);
    }

    [Fact]
    public async Task StopsEndlessRecursion()
    {
        var result = await _runner.RunTestsAsync(
            "public static class Calc { public static long Sum(ArrayList numbers) => Sum(numbers); }",
            SumTests);

        Assert.Equal(RunStatus.Stopped, result.Status);
    }

    [Fact]
    public async Task KeepsStoppingWhenTheReaderCatchesTheStop()
    {
        var result = await _runner.RunTestsAsync(
            """
            public static class Calc
            {
                public static long Sum(ArrayList numbers)
                {
                    while (true)
                    {
                        try { while (true) { } } catch (Exception) { }
                    }
                }
            }
            """,
            SumTests);

        Assert.Equal(RunStatus.Stopped, result.Status);
    }

    [Fact]
    public async Task ReportsAnUnexpectedException()
    {
        var result = await _runner.RunTestsAsync(
            "public static class Calc { public static long Sum(ArrayList numbers) => (long)numbers[0]!; }",
            "Assert.Equal(1L, Calc.Sum(new ArrayList { 1 }));");

        Assert.Equal(RunStatus.Crashed, result.Status);
        Assert.Contains("InvalidCastException", Assert.Single(result.Messages), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CapturesWhatTheCodePrints()
    {
        var result = await _runner.RunTestsAsync(
            "public static class Greeter { public static void Hello() => Console.WriteLine(\"hi\"); }",
            "Greeter.Hello();");

        Assert.Equal(RunStatus.Passed, result.Status);
        Assert.Equal("hi", result.Output.Trim());
    }

    [Fact]
    public async Task ChecksExpectedExceptions()
    {
        var result = await _runner.RunTestsAsync(
            "public static class Parser { public static int Parse(string s) => int.Parse(s); }",
            "Assert.Throws<FormatException>(() => Parser.Parse(\"x\"));");

        Assert.Equal(RunStatus.Passed, result.Status);
    }
}
