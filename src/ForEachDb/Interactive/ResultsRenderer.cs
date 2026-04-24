using ForEachDbQueries;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace ForEachDb.Interactive;

public static class ResultsRenderer
{
    private const int PreviewRows = 50;

    public static void PrintSummary(IReadOnlyList<DatabaseRow> rows)
    {
        if (rows.Count == 0) return;

        var aggregated = ResultsAggregator.Aggregate(rows);
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title($"[bold]Results[/] · {aggregated.Rows.Count} row(s) from {CountDbs(rows)} database(s)");

        foreach (var column in aggregated.Columns)
            table.AddColumn(new TableColumn($"[bold cyan]{Markup.Escape(column)}[/]"));

        foreach (var row in aggregated.Rows.Take(PreviewRows))
        {
            var cells = row
                .Select(v => v?.ToString() ?? string.Empty)
                .Select(s => (IRenderable)new Markup(Markup.Escape(Truncate(s, 60))))
                .ToArray();
            table.AddRow(cells);
        }

        AnsiConsole.Write(table);

        if (aggregated.Rows.Count > PreviewRows)
            AnsiConsole.MarkupLineInterpolated(
                $"[yellow]Showing {PreviewRows} of {aggregated.Rows.Count} rows — export CSV to see all.[/]");
    }

    public static async Task ExportCsvAsync(IReadOnlyList<DatabaseRow> rows)
    {
        if (rows.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No results to export.[/]");
            return;
        }

        var defaultPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            $"pgForEachDb-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

        var path = AnsiConsole.Prompt(new TextPrompt<string>("Save CSV to:")
            .DefaultValue(defaultPath)
            .ShowDefaultValue());

        if (!path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) path += ".csv";

        try
        {
            await using var stream = File.Create(path);
            await CsvExporter.WriteAsync(stream, rows);
            AnsiConsole.MarkupLineInterpolated($"[green]✓[/] Exported {rows.Count} row(s) to [cyan]{path}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Export failed:[/] {ex.Message}");
        }
    }

    private static int CountDbs(IReadOnlyList<DatabaseRow> rows)
    {
        var set = new HashSet<string>();
        foreach (var r in rows) set.Add(r.Database);
        return set.Count;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
