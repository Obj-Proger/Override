using Override.Content.Checking;
using Override.Content.Model;

namespace Override.Content.Tests;

public sealed class AnswerCheckerTests
{
    [Fact]
    public void ChoiceCountsOptionsFromZero()
    {
        var quiz = new ChoiceQuiz("q", null, "Pick.", null, null, ["a", "b"], 1);

        Assert.True(AnswerChecker.Check(quiz, 1));
        Assert.False(AnswerChecker.Check(quiz, 0));
    }

    [Theory]
    [InlineData("нечетное")]
    [InlineData("нечётное")]
    [InlineData("НЕЧЁТНОЕ")]
    public void TreatsYoAsYe(string answer)
    {
        var quiz = new OutputQuiz("q", null, "Print.", null, null, ["нечётное"], false);

        Assert.True(AnswerChecker.Check(quiz, answer));
    }

    [Fact]
    public void MultiNeedsExactlyTheCorrectOptions()
    {
        var quiz = new MultiQuiz("q", null, "Pick.", null, null, ["a", "b", "c"], [0, 2]);

        Assert.True(AnswerChecker.Check(quiz, new[] { 2, 0 }));
        Assert.False(AnswerChecker.Check(quiz, new[] { 0 }));
        Assert.False(AnswerChecker.Check(quiz, new[] { 0, 1, 2 }));
    }

    [Theory]
    [InlineData("123", true)]
    [InlineData("  123 \n", true)]
    [InlineData("12 3", false)]
    [InlineData("", false)]
    public void TypedAnswerIgnoresSpacesAtTheEnds(string answer, bool expected)
    {
        var quiz = new OutputQuiz("q", null, "Print?", "code", null, ["123"], false);

        Assert.Equal(expected, AnswerChecker.Check(quiz, answer));
    }

    [Fact]
    public void MultilineOutputComparesLineByLine()
    {
        var quiz = new OutputQuiz("q", null, "Print?", "code", null, ["0\n1"], false);

        Assert.True(AnswerChecker.Check(quiz, "0  \r\n1\n\n"));
        Assert.False(AnswerChecker.Check(quiz, "0 1"));
    }

    [Fact]
    public void CaseMattersOnlyWhenAsked()
    {
        var relaxed = new InputQuiz("q", null, "Type.", null, null, ["Value", "Other"], false);
        var strict = new InputQuiz("q", null, "Type.", null, null, ["Value"], true);

        Assert.True(AnswerChecker.Check(relaxed, "other"));
        Assert.True(AnswerChecker.Check(strict, "Value"));
        Assert.False(AnswerChecker.Check(strict, "value"));
    }

    [Fact]
    public void ParsonsNeedsTheLinesInOrderWithoutDistractors()
    {
        var quiz = new ParsonsQuiz("q", null, "Order.", null, null, ["a", "b"], ["x"]);

        Assert.True(AnswerChecker.Check(quiz, new[] { "a", "b" }));
        Assert.False(AnswerChecker.Check(quiz, new[] { "b", "a" }));
        Assert.False(AnswerChecker.Check(quiz, new[] { "a", "x" }));
    }

    [Fact]
    public void FindBugCountsLinesFromOne()
    {
        var quiz = new FindBugQuiz("q", null, "Where?", "a\nb", null, 2);

        Assert.True(AnswerChecker.Check(quiz, 2));
        Assert.False(AnswerChecker.Check(quiz, 1));
    }

    [Fact]
    public void RecallIsNotCheckedBySiteAndCodeRuns()
    {
        Assert.False(AnswerChecker.IsChecked(new RecallQuiz("q", null, "Explain.", null, null, "Answer.")));
        Assert.True(AnswerChecker.IsChecked(new FindBugQuiz("q", null, "Where?", "a", null, 1)));
        Assert.True(AnswerChecker.Runs(new CodeQuiz("q", null, "Write.", null, null, "csharp", "", "", "")));
    }
}
