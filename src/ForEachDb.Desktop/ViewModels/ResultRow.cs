namespace ForEachDb.Desktop.ViewModels;

public sealed class ResultRow
{
    public ResultRow(IReadOnlyList<string> columns, object?[] values)
    {
        var cells = new List<ResultCell>(columns.Count);
        for (var i = 0; i < columns.Count; i++)
            cells.Add(new ResultCell(columns[i], values[i]?.ToString() ?? string.Empty));
        Cells = cells;
    }

    public IReadOnlyList<ResultCell> Cells { get; }
}

public sealed record ResultCell(string Column, string Value);
