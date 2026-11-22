namespace Override.Content.Checking;

/// <summary>A query result: column names and rows, every value as text.</summary>
public sealed record SqlTable(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string>> Rows);

/// <summary>
/// Compares a query result with the result of the reference solution. Column names do not matter, values do.
/// Without a required order the rows are compared as a multiset, so any ORDER BY or none is fine.
/// </summary>
public static class SqlComparer
{
    private const char Separator = '\u001F';

    /// <summary>Null when the results match; otherwise what differs, in words for the reader.</summary>
    public static string? Difference(SqlTable expected, SqlTable actual, bool ordered)
    {
        if (expected.Columns.Count != actual.Columns.Count)
            return $"Ожидалось столбцов: {expected.Columns.Count}, получено: {actual.Columns.Count}.";

        if (expected.Rows.Count != actual.Rows.Count)
            return $"Ожидалось строк: {expected.Rows.Count}, получено: {actual.Rows.Count}.";

        var expectedRows = expected.Rows.Select(Key).ToList();
        var actualRows = actual.Rows.Select(Key).ToList();

        var sameRows = expectedRows.Order(StringComparer.Ordinal).SequenceEqual(actualRows.Order(StringComparer.Ordinal));
        if (!sameRows)
            return "Количество строк совпадает, но значения в них отличаются от ожидаемых.";

        if (!ordered || expectedRows.SequenceEqual(actualRows))
            return null;

        var first = expectedRows.Zip(actualRows).TakeWhile(pair => pair.First == pair.Second).Count() + 1;
        return $"Строки те же, но порядок другой, а здесь он важен: расхождение начинается со строки {first}.";
    }

    private static string Key(IReadOnlyList<string> row) => string.Join(Separator, row);
}
