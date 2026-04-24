using ForEachDbQueries;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace ForEachDb.Interactive;

/// <summary>
/// Interactive paged viewer for the captured results of the last run.
/// Uses Live + Layout so the table and status bar refresh in place.
/// </summary>
public static class ResultsViewer
{
    public static async Task ShowAsync(IReadOnlyList<DatabaseRow> rows)
    {
        if (rows.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No results to view.[/]");
            return;
        }

        var databases = rows.Select(r => r.Database).Distinct().OrderBy(d => d).ToList();
        string? dbFilter = null;
        var page = 0;

        while (true)
        {
            var filtered = dbFilter is null ? rows : rows.Where(r => r.Database == dbFilter).ToList();
            var aggregated = ResultsAggregator.Aggregate(filtered);
            var pageSize = Math.Max(5, Console.WindowHeight - 8);
            var totalPages = Math.Max(1, (aggregated.Rows.Count + pageSize - 1) / pageSize);
            page = Math.Clamp(page, 0, totalPages - 1);

            AnsiConsole.Clear();
            AnsiConsole.Write(BuildTable(aggregated, page, pageSize, dbFilter));
            AnsiConsole.Write(BuildStatus(page, totalPages, aggregated.Rows.Count, dbFilter));

            var key = Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case ConsoleKey.Q or ConsoleKey.Escape:
                    AnsiConsole.Clear();
                    return;
                case ConsoleKey.N or ConsoleKey.RightArrow or ConsoleKey.PageDown:
                    if (page < totalPages - 1) page++;
                    break;
                case ConsoleKey.P or ConsoleKey.LeftArrow or ConsoleKey.PageUp:
                    if (page > 0) page--;
                    break;
                case ConsoleKey.Home:
                    page = 0;
                    break;
                case ConsoleKey.End:
                    page = totalPages - 1;
                    break;
                case ConsoleKey.D:
                    dbFilter = PromptDbFilter(databases, dbFilter);
                    page = 0;
                    break;
                case ConsoleKey.E:
                    await ResultsRenderer.ExportCsvAsync(filtered);
                    AnsiConsole.MarkupLine("[dim]Press any key to continue...[/]");
                    Console.ReadKey(intercept: true);
                    break;
            }
        }
    }

    private static Table BuildTable(AggregatedResults aggregated, int page, int pageSize, string? dbFilter)
    {
        var title = dbFilter is null
            ? $"[bold]Results[/] · {aggregated.Rows.Count} row(s)"
            : $"[bold]Results[/] · {aggregated.Rows.Count} row(s) · db = [cyan]{Markup.Escape(dbFilter)}[/]";

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title(title)
            .Expand();

        foreach (var column in aggregated.Columns)
            table.AddColumn(new TableColumn($"[bold cyan]{Markup.Escape(column)}[/]"));

        var start = page * pageSize;
        var end = Math.Min(start + pageSize, aggregated.Rows.Count);
        for (var i = start; i < end; i++)
        {
            var cells = aggregated.Rows[i]
                .Select(v => v?.ToString() ?? string.Empty)
                .Select(s => (IRenderable)new Markup(Markup.Escape(Truncate(s, 80))))
                .ToArray();
            table.AddRow(cells);
        }

        return table;
    }

    private static IRenderable BuildStatus(int page, int totalPages, int rowCount, string? dbFilter)
    {
        var filter = dbFilter ?? "all";
        var status =
            $"[dim]page[/] {page + 1}/{totalPages}  " +
            $"[dim]rows[/] {rowCount}  " +
            $"[dim]filter[/] {Markup.Escape(filter)}  " +
            "[grey]·[/]  " +
            "[cyan]n[/]ext  [cyan]p[/]rev  [cyan]d[/]b filter  [cyan]e[/]xport  [cyan]q[/]uit";
        return new Markup(status);
    }

    private static string? PromptDbFilter(IReadOnlyList<string> databases, string? current)
    {
        const string all = "(all databases)";
        var choices = new List<string> { all };
        choices.AddRange(databases);

        var pick = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Filter by database:")
            .PageSize(Math.Min(20, choices.Count + 1))
            .AddChoices(choices));

        return pick == all ? null : pick;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
