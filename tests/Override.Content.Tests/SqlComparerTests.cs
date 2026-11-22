using Override.Content.Checking;

namespace Override.Content.Tests;

public sealed class SqlComparerTests
{
    private static SqlTable Table(params string[][] rows) => new(["a", "b"], rows);

    [Fact]
    public void SameRowsInAnyOrderMatchWhenOrderIsFree() =>
        Assert.Null(SqlComparer.Difference(Table(["1", "x"], ["2", "y"]), Table(["2", "y"], ["1", "x"]), ordered: false));

    [Fact]
    public void OrderMattersWhenRequired()
    {
        var difference = SqlComparer.Difference(Table(["1", "x"], ["2", "y"]), Table(["2", "y"], ["1", "x"]), ordered: true);

        Assert.Contains("порядок другой", difference, StringComparison.Ordinal);
    }

    [Fact]
    public void ColumnNamesDoNotMatter() =>
        Assert.Null(SqlComparer.Difference(Table(["1", "x"]), new SqlTable(["id", "name"], [["1", "x"]]), ordered: true));

    [Fact]
    public void ReportsDifferentColumnCounts() =>
        Assert.Contains(
            "столбцов",
            SqlComparer.Difference(Table(["1", "x"]), new SqlTable(["a"], [["1"]]), ordered: false),
            StringComparison.Ordinal);

    [Fact]
    public void ReportsDifferentRowCounts() =>
        Assert.Contains("строк", SqlComparer.Difference(Table(["1", "x"]), Table(), ordered: false), StringComparison.Ordinal);

    [Fact]
    public void ReportsDifferentValues() =>
        Assert.Contains(
            "значения",
            SqlComparer.Difference(Table(["1", "x"], ["1", "x"]), Table(["1", "x"], ["2", "x"]), ordered: false),
            StringComparison.Ordinal);
}
