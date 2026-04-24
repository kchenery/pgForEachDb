using System.Data;
using ForEachDbQueries;
using Terminal.Gui;

namespace ForEachDb.Tui.Views;

public static class ResultsDialog
{
    public const int PreviewCap = 10_000;

    public static void Show(IReadOnlyList<DatabaseRow> rows, Action<string>? log = null)
    {
        if (rows.Count == 0)
        {
            log?.Invoke("Results: no rows returned.");
            return;
        }

        var aggregated = ResultsAggregator.Aggregate(rows);
        var truncated = aggregated.Rows.Count > PreviewCap;
        var preview = truncated ? aggregated.Rows.Take(PreviewCap).ToList() : aggregated.Rows.ToList();

        var table = BuildTable(aggregated.Columns, preview);

        var close = new Button("Close", is_default: true);
        var export = new Button("Export CSV (Ctrl+E)");

        var dialog = new Dialog($"Results — {aggregated.Rows.Count} row(s) from {CountDatabases(rows)} database(s)", 100, 28, export, close);

        var banner = new Label(truncated
            ? $"Preview truncated to {PreviewCap:N0} of {aggregated.Rows.Count:N0} rows — export CSV for the full set."
            : $"Showing {preview.Count:N0} row(s).")
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill() - 2,
            Height = 1,
            ColorScheme = truncated ? Colors.Error : Colors.Dialog
        };

        var tableView = new TableView
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 2,
            Table = table,
            FullRowSelect = true
        };

        dialog.Add(banner, tableView);

        close.Clicked += () => Application.RequestStop();
        export.Clicked += () => ExportCsv(rows, log);
        dialog.KeyPress += e =>
        {
            if (e.KeyEvent.Key == (Key.E | Key.CtrlMask))
            {
                ExportCsv(rows, log);
                e.Handled = true;
            }
        };

        Application.Run(dialog);
    }

    private static DataTable BuildTable(IReadOnlyList<string> columns, IReadOnlyList<object?[]> rows)
    {
        var table = new DataTable();
        foreach (var column in columns)
            table.Columns.Add(column, typeof(string));

        foreach (var row in rows)
        {
            var stringified = new object[row.Length];
            for (var i = 0; i < row.Length; i++)
                stringified[i] = row[i]?.ToString() ?? string.Empty;
            table.Rows.Add(stringified);
        }

        return table;
    }

    private static int CountDatabases(IReadOnlyList<DatabaseRow> rows)
    {
        var set = new HashSet<string>();
        foreach (var row in rows) set.Add(row.Database);
        return set.Count;
    }

    private static void ExportCsv(IReadOnlyList<DatabaseRow> rows, Action<string>? log)
    {
        var save = new SaveDialog("Export CSV", "Choose a destination file")
        {
            AllowedFileTypes = [".csv"]
        };

        Application.Run(save);

        if (save.Canceled || save.FilePath is null)
            return;

        var path = save.FilePath.ToString();
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) path += ".csv";

        try
        {
            using var stream = File.Create(path);
            CsvExporter.WriteAsync(stream, rows).GetAwaiter().GetResult();
            log?.Invoke($"Exported {rows.Count} row(s) to {path}");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Export failed", ex.Message, "Close");
        }
    }
}
