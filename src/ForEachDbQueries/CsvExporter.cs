using System.Globalization;

namespace ForEachDbQueries;

public static class CsvExporter
{
    private const string DatabaseColumn = "database";

    public static async Task WriteAsync(
        Stream destination,
        IEnumerable<DatabaseRow> rows,
        CancellationToken cancellationToken = default)
    {
        var buffered = rows.ToList();
        var columns = BuildColumnOrder(buffered);

        await using var writer = new StreamWriter(destination, leaveOpen: true);

        await writer.WriteLineAsync(
            new ReadOnlyMemory<char>(string.Join(",", columns.Select(EscapeField)).ToCharArray()),
            cancellationToken);

        foreach (var row in buffered)
        {
            var fields = columns.Select(column =>
            {
                if (column == DatabaseColumn) return row.Database;
                return row.Values.TryGetValue(column, out var value) ? Format(value) : string.Empty;
            });

            await writer.WriteLineAsync(
                new ReadOnlyMemory<char>(string.Join(",", fields.Select(EscapeField)).ToCharArray()),
                cancellationToken);
        }

        await writer.FlushAsync(cancellationToken);
    }

    public static IReadOnlyList<string> BuildColumnOrder(IEnumerable<DatabaseRow> rows)
    {
        var columns = new List<string> { DatabaseColumn };
        var seen = new HashSet<string>(StringComparer.Ordinal) { DatabaseColumn };

        foreach (var row in rows)
        {
            foreach (var key in row.Values.Keys)
            {
                if (seen.Add(key)) columns.Add(key);
            }
        }

        return columns;
    }

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        string s => s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static string EscapeField(string value)
    {
        if (value.Length == 0) return value;

        var needsQuoting = value.IndexOfAny(['"', ',', '\n', '\r']) >= 0;
        if (!needsQuoting) return value;

        return string.Concat("\"", value.Replace("\"", "\"\""), "\"");
    }
}
