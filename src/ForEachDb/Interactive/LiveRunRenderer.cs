using System.Collections.Concurrent;
using ForEachDbQueries;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace ForEachDb.Interactive;

/// <summary>
/// Two-panel live display for a single run: database states on the left, log stream on the right,
/// with a status footer.
/// </summary>
public sealed class LiveRunRenderer
{
    private static readonly string[] Spinner =
        ["⠋","⠙","⠹","⠸","⠼","⠴","⠦","⠧","⠇","⠏"];

    private const int MaxLogLines = 200;

    private readonly ConcurrentDictionary<string, DatabaseStatus> _statuses = new();
    private readonly List<DatabaseLogEntry> _log = new();
    private readonly object _logLock = new();
    private readonly IReadOnlyList<string> _selected;
    private readonly RunSession _session;
    private readonly DateTime _start = DateTime.UtcNow;
    private int _frame;

    public LiveRunRenderer(RunSession session, IReadOnlyList<string> selected)
    {
        _session = session;
        _selected = selected;
        foreach (var db in selected)
            _statuses[db] = new DatabaseStatus(db, DatabaseRunState.Pending);
    }

    public async Task<RunOutcome> RunAsync(string query)
    {
        _session.StatusChanged += OnStatus;
        _session.LogEntryAppended += OnLog;

        using var cancelHandler = new ConsoleCancelHandler(_session.Cancel);

        try
        {
            RunOutcome outcome = RunOutcome.EmptyQuery;
            var layout = BuildLayout();

            var renderTask = AnsiConsole.Live(layout)
                .AutoClear(false)
                .Overflow(VerticalOverflow.Ellipsis)
                .Cropping(VerticalOverflowCropping.Bottom)
                .StartAsync(async ctx =>
                {
                    using var ticker = new Timer(_ =>
                    {
                        Interlocked.Increment(ref _frame);
                        ctx.UpdateTarget(BuildLayout());
                    }, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));

                    outcome = await _session.RunAsync(query, _selected);
                    ctx.UpdateTarget(BuildLayout());
                });

            await renderTask;
            return outcome;
        }
        finally
        {
            _session.StatusChanged -= OnStatus;
            _session.LogEntryAppended -= OnLog;
        }
    }

    private void OnStatus(DatabaseStatus s) => _statuses[s.DatabaseName] = s;

    private void OnLog(DatabaseLogEntry e)
    {
        lock (_logLock)
        {
            _log.Add(e);
            if (_log.Count > MaxLogLines)
                _log.RemoveRange(0, _log.Count - MaxLogLines);
        }
    }

    private Layout BuildLayout()
    {
        var layout = new Layout("root")
            .SplitRows(
                new Layout("body").SplitColumns(
                    new Layout("db").Ratio(2),
                    new Layout("log").Ratio(3)),
                new Layout("footer").Size(3));

        layout["db"].Update(new Panel(BuildDbList())
            .Header("[bold]Databases[/]")
            .Expand());
        layout["log"].Update(new Panel(BuildLog())
            .Header("[bold]Log[/]")
            .Expand());
        layout["footer"].Update(new Panel(BuildFooter())
            .Border(BoxBorder.Rounded)
            .Expand());

        return layout;
    }

    private IRenderable BuildDbList()
    {
        var spinner = Spinner[Volatile.Read(ref _frame) % Spinner.Length];
        var rows = new List<IRenderable>();

        foreach (var db in _selected)
        {
            var s = _statuses.TryGetValue(db, out var v) ? v : new DatabaseStatus(db, DatabaseRunState.Pending);
            rows.Add(new Markup(RenderDb(s, spinner)));
        }

        return new Rows(rows);
    }

    private static string RenderDb(DatabaseStatus s, string spinner)
    {
        var name = Markup.Escape(s.DatabaseName);
        return s.State switch
        {
            DatabaseRunState.Pending   => $"[dim]○ {name}[/]",
            DatabaseRunState.Running   => $"[yellow]{spinner}[/] {name}",
            DatabaseRunState.Succeeded => $"[green]✔[/] {name}{Duration(s)}",
            DatabaseRunState.Failed    => $"[red]✘[/] {name} [dim]- {Markup.Escape(Truncate(s.ErrorMessage ?? "error", 60))}[/]",
            DatabaseRunState.Cancelled => $"[grey]◐[/] {name} [dim]cancelled[/]",
            _ => name
        };
    }

    private static string Duration(DatabaseStatus s) =>
        s.Duration is { } d ? $" [dim]{d.TotalSeconds:F1}s[/]" : string.Empty;

    private IRenderable BuildLog()
    {
        lock (_logLock)
        {
            if (_log.Count == 0)
                return new Markup("[dim]No log entries yet…[/]");

            var lines = _log
                .TakeLast(80)
                .Select(FormatLogEntry)
                .Select(text => (IRenderable)new Markup(text));
            return new Rows(lines);
        }
    }

    private static string FormatLogEntry(DatabaseLogEntry e)
    {
        var time = e.Timestamp.LocalDateTime.ToString("HH:mm:ss");
        var (tag, style) = e.Level switch
        {
            DatabaseLogLevel.Notice  => ("NOTICE", "cyan"),
            DatabaseLogLevel.Warning => ("WARN",   "yellow"),
            DatabaseLogLevel.Error   => ("ERROR",  "red"),
            _                        => ("INFO",   "dim")
        };
        return $"[dim]{time}[/] [bold]{Markup.Escape(e.DatabaseName),-16}[/] [{style}]{tag,-6}[/] {Markup.Escape(e.Message)}";
    }

    private IRenderable BuildFooter()
    {
        var done = _statuses.Values.Count(s => s.State == DatabaseRunState.Succeeded);
        var failed = _statuses.Values.Count(s => s.State == DatabaseRunState.Failed);
        var cancelled = _statuses.Values.Count(s => s.State == DatabaseRunState.Cancelled);
        var running = _statuses.Values.Count(s => s.State == DatabaseRunState.Running);
        var total = _selected.Count;
        var elapsed = DateTime.UtcNow - _start;

        var summary = $"[green]{done}[/]/{total} done · [red]{failed}[/] failed · [grey]{cancelled}[/] cancelled · [yellow]{running}[/] running";
        var time = $"elapsed [bold]{elapsed.TotalSeconds:F0}s[/]";
        var hint = _session.IsRunning ? "[dim]Ctrl+C to cancel[/]" : "[green]complete[/]";

        return new Markup($"{summary}  ·  {time}  ·  {hint}");
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    private sealed class ConsoleCancelHandler : IDisposable
    {
        private readonly Action _cancel;
        public ConsoleCancelHandler(Action cancel)
        {
            _cancel = cancel;
            Console.CancelKeyPress += Handler;
        }

        private void Handler(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            _cancel();
        }

        public void Dispose() => Console.CancelKeyPress -= Handler;
    }
}
