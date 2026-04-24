using ForEachDb.Tui.Models;
using ForEachDbQueries;
using Npgsql;

namespace ForEachDb.Tui.Sessions;

/// <summary>
/// Owns the run lifecycle (selection-independent state, cancellation, results, statuses, log stream)
/// so it can be unit tested without Terminal.Gui. MainWindow subscribes to events and marshals them
/// onto the UI thread.
/// </summary>
public sealed class RunSession
{
    private readonly IForEachDbRunner _runner;
    private readonly Dictionary<string, DatabaseStatus> _statuses = new();
    private CancellationTokenSource? _cts;

    public ConnectionSettings Settings { get; }
    public IReadOnlyList<string> Databases { get; }
    public int Threads { get; set; } = 4;
    public bool IsRunning { get; private set; }
    public DateTime? RunStartedAt { get; private set; }
    public IReadOnlyList<DatabaseRow> LastResults { get; private set; } = Array.Empty<DatabaseRow>();
    public IReadOnlyDictionary<string, DatabaseStatus> Statuses => _statuses;

    public event Action<DatabaseStatus>? StatusChanged;
    public event Action<DatabaseLogEntry>? LogEntryAppended;
    public event Action? RunStarted;
    public event Action<RunOutcome>? RunCompleted;

    public RunSession(
        ConnectionSettings settings,
        IReadOnlyList<string> databases,
        IForEachDbRunner? runner = null)
    {
        Settings = settings;
        Databases = databases;
        _runner = runner ?? new ForEachDbRunner(BuildConnectionString(settings));
    }

    public async Task<RunOutcome> RunAsync(string? query, IReadOnlyList<string> selection)
    {
        if (IsRunning) return RunOutcome.AlreadyRunning;

        var trimmed = (query ?? string.Empty).Trim();
        if (trimmed.Length == 0) return RunOutcome.EmptyQuery;
        if (selection.Count == 0) return RunOutcome.NothingSelected;

        _statuses.Clear();
        LastResults = Array.Empty<DatabaseRow>();
        IsRunning = true;
        RunStartedAt = DateTime.UtcNow;
        _cts = new CancellationTokenSource();

        var progress = new ProgressRelay(s =>
        {
            _statuses[s.DatabaseName] = s;
            StatusChanged?.Invoke(s);
        });
        var sink = new LogRelay(e => LogEntryAppended?.Invoke(e));

        RunStarted?.Invoke();

        RunOutcome outcome;
        try
        {
            var rows = await _runner.RunQueryAsDynamicAsync(
                selection, trimmed, Threads, progress, sink, _cts.Token);
            LastResults = rows;
            outcome = RunOutcome.Completed;
        }
        catch (OperationCanceledException)
        {
            outcome = RunOutcome.Cancelled;
        }
        catch (Exception ex)
        {
            outcome = RunOutcome.Failed(ex.Message);
        }
        finally
        {
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }

        RunCompleted?.Invoke(outcome);
        return outcome;
    }

    public void Cancel() => _cts?.Cancel();

    private static string BuildConnectionString(ConnectionSettings s) =>
        new NpgsqlConnectionStringBuilder
        {
            Host = s.Host,
            Port = s.Port,
            Database = s.Database,
            Username = s.Username,
            Password = s.Password
        }.ConnectionString;

    private sealed class ProgressRelay : IProgress<DatabaseStatus>
    {
        private readonly Action<DatabaseStatus> _on;
        public ProgressRelay(Action<DatabaseStatus> on) => _on = on;
        public void Report(DatabaseStatus value) => _on(value);
    }

    private sealed class LogRelay : IDatabaseLogSink
    {
        private readonly Action<DatabaseLogEntry> _on;
        public LogRelay(Action<DatabaseLogEntry> on) => _on = on;
        public void Append(DatabaseLogEntry entry) => _on(entry);
    }
}
