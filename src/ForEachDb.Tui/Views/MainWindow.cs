using ForEachDb.Tui.Infrastructure;
using ForEachDb.Tui.Models;
using ForEachDb.Tui.Sessions;
using ForEachDbQueries;
using Terminal.Gui;

namespace ForEachDb.Tui.Views;

public sealed class MainWindow : Window
{
    private readonly ConnectionResult _connection;
    private readonly RunSession _session;
    private readonly DatabaseListView _databases;
    private readonly LogView _log;
    private readonly SqlEditorView _editor;
    private readonly Label _status;

    private object? _spinnerToken;
    private object? _timerToken;

    public MainWindow(ConnectionResult connection) : base(BuildTitle(connection.Settings))
    {
        _connection = connection;
        _session = new RunSession(connection.Settings, connection.Databases);

        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        _databases = new DatabaseListView(connection.Databases)
        {
            X = 0, Y = 0, Width = Dim.Percent(35), Height = Dim.Fill() - 8
        };
        _log = new LogView
        {
            X = Pos.Right(_databases), Y = 0, Width = Dim.Fill(), Height = Dim.Fill() - 8
        };
        _editor = new SqlEditorView
        {
            X = 0, Y = Pos.Bottom(_databases), Width = Dim.Fill(), Height = 7
        };
        _status = new Label(string.Empty)
        {
            X = 0, Y = Pos.AnchorEnd(1), Width = Dim.Fill(), Height = 1
        };

        Add(_databases, _log, _editor, _status);

        _databases.SelectionChanged += () =>
        {
            _log.SetSelectedDatabase(_databases.SelectedRowDatabase);
            RefreshStatus();
        };
        _databases.RowActivated += OnRowActivated;

        _session.StatusChanged += s => OnUi(() => _databases.ApplyStatus(s));
        _session.LogEntryAppended += e => OnUi(() => _log.Append(e));
        _session.RunStarted += () => OnUi(OnRunStarted);
        _session.RunCompleted += outcome => OnUi(() => OnRunCompleted(outcome));

        InstallShortcuts();
        StartSpinner();
        RefreshStatus();

        _log.Append($"Connected to {connection.Settings.Host}:{connection.Settings.Port} as {connection.Settings.Username}.");
        _log.Append($"Found {connection.Databases.Count} database(s). F5 run, F6 cancel, Ctrl+R results, Ctrl+T threads, Ctrl+L filter, Ctrl+S save recipe.");

        if (connection.Recipe is { } recipe)
            ApplyRecipe(recipe);
    }

    // Minimal test seams — only what the shortcut-routing tests need to assert against.
    internal int SelectedDatabaseCount => _databases.SelectedCount;
    internal LogFilter CurrentLogFilter => _log.Filter;
    internal int Threads => _session.Threads;

    public Task RunAsync() => OnRunRequestedAsync();
    public void RequestCancel() => _session.Cancel();
    public void OpenThreadsDialog() => OpenThreadsDialogInternal();
    public void ShowResults() => ShowResultsInternal();
    public void CycleFilter() => CycleFilterInternal();
    public void SaveRecipe() => SaveRecipeInternal();
    public void Quit() => QuitInternal();

    private void InstallShortcuts()
    {
        // Global shortcuts — fire regardless of which child view has focus.
        // Skip while a modal dialog is on top so it keeps its own key routing.
        Application.RootKeyEvent = ke =>
        {
            if (Application.Current is Dialog) return false;
            return HandleShortcut(ke.Key);
        };
    }

    private bool HandleShortcut(Key key)
    {
        switch (key)
        {
            case Key.Esc:
            case Key.Q | Key.CtrlMask:
                QuitInternal();
                return true;
            case Key.F5:
            case Key.Enter | Key.CtrlMask:
                _ = OnRunRequestedAsync();
                return true;
            case Key.F6:
                _session.Cancel();
                return true;
            case Key.A | Key.CtrlMask:
                _databases.SelectAll();
                return true;
            case Key.N | Key.CtrlMask:
                _databases.SelectNone();
                return true;
            case Key.T | Key.CtrlMask:
                OpenThreadsDialogInternal();
                return true;
            case Key.L | Key.CtrlMask:
                CycleFilterInternal();
                return true;
            case Key.R | Key.CtrlMask:
                ShowResultsInternal();
                return true;
            case Key.S | Key.CtrlMask:
                SaveRecipeInternal();
                return true;
            case Key.F1:
                HelpDialog.Show();
                return true;
        }
        return false;
    }

    private async Task OnRunRequestedAsync()
    {
        _databases.ResetStatuses();

        var query = _editor.Query;
        var selected = _databases.SelectedDatabases;

        _editor.RecordSubmit(query);

        var outcome = await _session.RunAsync(query, selected);

        switch (outcome.Kind)
        {
            case RunOutcomeKind.AlreadyRunning:
                _log.Append("Run: already running — F6 to cancel.");
                break;
            case RunOutcomeKind.EmptyQuery:
                _log.Append("Run: empty query — enter SQL in the editor.");
                break;
            case RunOutcomeKind.NothingSelected:
                _log.Append("Run: no databases selected.");
                break;
            case RunOutcomeKind.Failed:
                _log.Append($"Run failed: {outcome.Message}");
                break;
        }
    }

    private void OnRunStarted()
    {
        _log.Append($"Running across {_databases.SelectedCount} database(s) with {_session.Threads} thread(s)…");
        StartTimer();
        RefreshStatus();
    }

    private void OnRunCompleted(RunOutcome outcome)
    {
        StopTimer();

        var elapsed = _session.RunStartedAt is { } started ? DateTime.UtcNow - started : TimeSpan.Zero;
        _log.Append($"Run complete in {elapsed.TotalSeconds:F1}s — {Summary()}.");

        if (outcome.Kind == RunOutcomeKind.Completed && _session.LastResults.Count > 0)
            _log.Append($"Results: {_session.LastResults.Count} row(s) ready — Ctrl+R to view, Ctrl+E to export CSV.");

        RefreshStatus();
    }

    private void QuitInternal()
    {
        if (_session.IsRunning)
        {
            _session.Cancel();
            _log.Append("Cancelling active run — press again to quit.");
            return;
        }

        var answer = MessageBox.Query(
            width: 50,
            height: 7,
            title: "Quit pgForEachDb?",
            message: "Any unsaved query text will be lost.",
            "Quit", "Cancel");

        if (answer == 0)
            Application.RequestStop();
    }

    private void OpenThreadsDialogInternal()
    {
        if (_session.IsRunning)
        {
            _log.Append("Cannot change threads during a run.");
            return;
        }

        var value = ThreadsDialog.Prompt(_session.Threads);
        if (value is { } v)
        {
            _session.Threads = v;
            RefreshStatus();
        }
    }

    private void CycleFilterInternal()
    {
        var next = _log.Filter switch
        {
            LogFilter.All => LogFilter.SelectedDatabase,
            LogFilter.SelectedDatabase => LogFilter.FailedOnly,
            _ => LogFilter.All
        };
        _log.SetFilter(next);
        _log.Append($"Log filter: {next}");
    }

    private void ShowResultsInternal()
    {
        if (_session.LastResults.Count == 0)
        {
            _log.Append("Results: nothing to show yet — run a query that returns rows first.");
            return;
        }

        ResultsDialog.Show(_session.LastResults, _log.Append);
    }

    private void SaveRecipeInternal()
    {
        var template = BuildRecipeTemplate();
        if (SaveRecipeDialog.Prompt(new RecipeStore(), template, out var name))
            _log.Append($"Recipe \"{name}\" saved.");
    }

    private Recipe BuildRecipeTemplate() => new(
        Name: _connection.Recipe?.Name ?? string.Empty,
        Connection: _connection.Settings with { Password = string.Empty },
        SelectedDatabases: _databases.SelectedDatabases,
        Query: _editor.Query,
        Threads: _session.Threads);

    private void ApplyRecipe(Recipe recipe)
    {
        _session.Threads = Math.Clamp(recipe.Threads, 1, 64);

        var available = new HashSet<string>(_connection.Databases, StringComparer.Ordinal);
        _databases.SetSelection(recipe.SelectedDatabases);

        var missing = recipe.SelectedDatabases.Where(n => !available.Contains(n)).ToList();
        if (missing.Count > 0)
            _log.Append($"Recipe \"{recipe.Name}\": {missing.Count} saved DB(s) not found in this cluster — skipped: {string.Join(", ", missing)}.");

        _editor.SetQuery(recipe.Query);
        RefreshStatus();
        _log.Append($"Recipe \"{recipe.Name}\" applied: {_databases.SelectedCount} db selected, {_session.Threads} threads.");
    }

    private void OnRowActivated(string database)
    {
        if (_session.Statuses.TryGetValue(database, out var status) && status.State == DatabaseRunState.Failed)
            ErrorDetailDialog.Show(status);
    }

    private static void OnUi(Action action) => Application.MainLoop?.Invoke(action);

    private void StartSpinner()
    {
        _spinnerToken = Application.MainLoop?.AddTimeout(TimeSpan.FromMilliseconds(100), _ =>
        {
            _databases.TickSpinner();
            return true;
        });
    }

    private void StartTimer()
    {
        _timerToken = Application.MainLoop?.AddTimeout(TimeSpan.FromMilliseconds(500), _ =>
        {
            RefreshStatus();
            return _session.IsRunning;
        });
    }

    private void StopTimer()
    {
        if (_timerToken is not null)
        {
            Application.MainLoop?.RemoveTimeout(_timerToken);
            _timerToken = null;
        }
    }

    private string Summary()
    {
        var counts = new Dictionary<DatabaseRunState, int>
        {
            [DatabaseRunState.Pending] = 0,
            [DatabaseRunState.Running] = 0,
            [DatabaseRunState.Succeeded] = 0,
            [DatabaseRunState.Failed] = 0,
            [DatabaseRunState.Cancelled] = 0
        };
        foreach (var status in _session.Statuses.Values)
            counts[status.State]++;
        return $"{counts[DatabaseRunState.Succeeded]} done, {counts[DatabaseRunState.Failed]} failed, {counts[DatabaseRunState.Cancelled]} cancelled";
    }

    private void RefreshStatus()
    {
        var elapsed = _session.RunStartedAt is { } started ? DateTime.UtcNow - started : (TimeSpan?)null;
        var summary = Summary();
        var elapsedText = elapsed is { } e ? $"  —  elapsed {e.TotalSeconds:F0}s" : string.Empty;
        var runState = _session.IsRunning ? "RUNNING" : "idle";
        var resultsHint = _session.LastResults.Count > 0 ? $"  —  {_session.LastResults.Count} rows (Ctrl+R)" : string.Empty;
        _status.Text = $" {_databases.SelectedCount}/{_databases.TotalCount} selected  —  threads {_session.Threads}  —  {runState}  —  {summary}{elapsedText}{resultsHint}  —  F5 run  F6 cancel  Ctrl+R results  Ctrl+S save  Ctrl+T threads  Ctrl+L filter  Esc quit";
        _status.SetNeedsDisplay();
    }

    private static string BuildTitle(ConnectionSettings settings) =>
        $"pgForEachDb — {settings.Username}@{settings.Host}:{settings.Port}";
}
