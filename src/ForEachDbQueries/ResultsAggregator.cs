namespace ForEachDbQueries;

public sealed record AggregatedResults(
    IReadOnlyList<string> Columns,
    IReadOnlyList<object?[]> Rows);

public static class ResultsAggregator
{
    public static AggregatedResults Aggregate(IEnumerable<DatabaseRow> source)
    {
        var buffered = source.ToList();
        var columns = CsvExporter.BuildColumnOrder(buffered);

        var rows = new List<object?[]>(buffered.Count);
        foreach (var row in buffered)
        {
            var values = new object?[columns.Count];
            for (var i = 0; i < columns.Count; i++)
            {
                if (columns[i] == "database")
                {
                    values[i] = row.Database;
                    continue;
                }

                values[i] = row.Values.TryGetValue(columns[i], out var value) ? value : null;
            }
            rows.Add(values);
        }

        return new AggregatedResults(columns, rows);
    }
}
