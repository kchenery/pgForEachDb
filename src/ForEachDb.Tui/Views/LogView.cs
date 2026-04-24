using ForEachDbQueries;
using Terminal.Gui;

namespace ForEachDb.Tui.Views;

public enum LogFilter { All, SelectedDatabase, FailedOnly }

public sealed class LogView : FrameView
{
    private readonly TextView _text;
    private readonly List<LogEntry> _entries = new();
    private LogFilter _filter = LogFilter.All;
    private string? _selectedDatabase;

    public LogView() : base("Log (all)")
    {
        _text = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = false,
            CanFocus = true
        };

        Add(_text);
    }

    public void Append(string line)
    {
        _entries.Add(new LogEntry(null, DateTimeOffset.Now, DatabaseLogLevel.Info, line, IsAmbient: true));
        Rerender();
    }

    public void Append(DatabaseLogEntry entry)
    {
        _entries.Add(new LogEntry(entry.DatabaseName, entry.Timestamp, entry.Level, entry.Message, IsAmbient: false));
        Rerender();
    }

    public LogFilter Filter => _filter;

    // Exposed for the test project only (InternalsVisibleTo).
    internal string VisibleTextForTests => _text.Text?.ToString() ?? string.Empty;

    public void SetFilter(LogFilter filter)
    {
        _filter = filter;
        Title = filter switch
        {
            LogFilter.All => "Log (all)",
            LogFilter.SelectedDatabase => $"Log ({_selectedDatabase ?? "selected"})",
            LogFilter.FailedOnly => "Log (failed only)",
            _ => "Log"
        };
        Rerender();
    }

    public void SetSelectedDatabase(string? database)
    {
        _selectedDatabase = database;
        if (_filter == LogFilter.SelectedDatabase)
        {
            Title = $"Log ({database ?? "selected"})";
            Rerender();
        }
    }

    public void ClearEntries()
    {
        _entries.Clear();
        Rerender();
    }

    private void Rerender()
    {
        var filtered = _entries.Where(Matches);
        var text = string.Join(Environment.NewLine, filtered.Select(Format));
        _text.Text = text;
        _text.MoveEnd();
        _text.SetNeedsDisplay();
        SetNeedsDisplay();
    }

    private bool Matches(LogEntry entry) => _filter switch
    {
        LogFilter.All => true,
        LogFilter.SelectedDatabase => entry.IsAmbient || entry.Database == _selectedDatabase,
        LogFilter.FailedOnly => entry.Level is DatabaseLogLevel.Error or DatabaseLogLevel.Warning || entry.IsAmbient,
        _ => true
    };

    private static string Format(LogEntry entry)
    {
        var time = entry.Timestamp.LocalDateTime.ToString("HH:mm:ss");
        var lvl = entry.Level switch
        {
            DatabaseLogLevel.Notice => "NOTICE",
            DatabaseLogLevel.Warning => "WARN",
            DatabaseLogLevel.Error => "ERROR",
            _ => "INFO"
        };

        return entry.Database is null
            ? $"{time}        {entry.Message}"
            : $"{time} {entry.Database,-20} {lvl,-6} {entry.Message}";
    }

    private sealed record LogEntry(
        string? Database,
        DateTimeOffset Timestamp,
        DatabaseLogLevel Level,
        string Message,
        bool IsAmbient);
}
