using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForEachDb.Desktop.Services;
using ForEachDbQueries;

namespace ForEachDb.Desktop.ViewModels;

public sealed partial class WorkspaceViewModel : ViewModelBase
{
    private readonly RunSession _session;
    private readonly RecipeStore _recipes;
    private readonly ConnectionSettings _settings;
    private readonly IFileDialogService _fileDialogs;
    private IReadOnlyList<DatabaseRow> _rawResults = Array.Empty<DatabaseRow>();
    private int _threads = WorkspaceDefaults.DefaultThreads;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private string _query;

    [ObservableProperty]
    private string _runStatus = "Idle.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isSavingRecipe;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmSaveRecipeCommand))]
    private string _saveRecipeName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsShowingQuery))]
    [NotifyPropertyChangedFor(nameof(IsShowingResults))]
    [NotifyPropertyChangedFor(nameof(IsShowingLog))]
    private WorkspacePane _activePane = WorkspacePane.Query;

    [ObservableProperty]
    private string _selectionSummary = string.Empty;

    [ObservableProperty]
    private string _databaseSearch = string.Empty;

    [ObservableProperty]
    private bool _showTemplates;

    public FilterSelector ResultsFilter { get; } = new();
    public FilterSelector LogsFilter { get; } = new();

    public WorkspaceViewModel(
        ConnectionSettings settings,
        IReadOnlyList<DiscoveredDatabase> databases,
        Recipe? recipe,
        RecipeStore recipes,
        IFileDialogService fileDialogs)
    {
        _settings = settings;
        _recipes = recipes;
        _fileDialogs = fileDialogs;
        var names = databases.Select(d => d.Name).ToList();
        _session = new RunSession(settings, names);

        // Each Progress<T> captures the UI SynchronizationContext at construction
        // and marshals Report() callbacks back onto it.
        var statusProgress    = new Progress<DatabaseStatus>(OnStatusChanged);
        var startedProgress   = new Progress<bool>(_ => { IsRunning = true; RefreshRunStatus(); });
        var completedProgress = new Progress<RunOutcome>(_ => IsRunning = false);
        var logProgress       = new Progress<DatabaseLogEntry>(AppendLog);

        _session.StatusChanged    += ((IProgress<DatabaseStatus>)statusProgress).Report;
        _session.RunStarted       += () => ((IProgress<bool>)startedProgress).Report(true);
        _session.RunCompleted     += ((IProgress<RunOutcome>)completedProgress).Report;
        _session.LogEntryAppended += ((IProgress<DatabaseLogEntry>)logProgress).Report;

        _query = recipe?.Query ?? string.Empty;
        _threads = recipe?.Threads ?? WorkspaceDefaults.DefaultThreads;
        _session.Threads = _threads;
        _initialRecipeName = recipe?.Name;

        // Default selection: all non-template databases. A loaded recipe overrides this.
        var preselected = recipe is not null
            ? new HashSet<string>(recipe.SelectedDatabases, StringComparer.Ordinal)
            : new HashSet<string>(databases.Where(d => !d.IsTemplate).Select(d => d.Name), StringComparer.Ordinal);

        foreach (var d in databases)
            Databases.Add(new DatabaseItem(d.Name, d.IsTemplate, preselected.Contains(d.Name), OnSelectionChanged));

        // If a recipe pre-selects template databases, surface them by default.
        if (Databases.Any(d => d.IsTemplate && d.IsSelected))
            _showTemplates = true;

        RefreshVisibility();
        RefreshSelectionSummary();

        Results.CollectionChanged += (_, _) => ExportCsvCommand.NotifyCanExecuteChanged();
        Logs.CollectionChanged += (_, _) => ExportLogCommand.NotifyCanExecuteChanged();

        ResultsFilter.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FilterSelector.Selected)) ApplyResultFilter();
        };
        LogsFilter.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FilterSelector.Selected)) RefreshLogVisibility();
        };
    }

    partial void OnDatabaseSearchChanged(string value) => RefreshVisibility();
    partial void OnShowTemplatesChanged(bool value) => RefreshVisibility();

    private void RefreshVisibility()
    {
        foreach (var item in Databases)
            item.IsVisible = DatabaseVisibility.Matches(item.Name, item.IsTemplate, DatabaseSearch, ShowTemplates);
    }

    private readonly string? _initialRecipeName;

    public event Action? Disconnected;

    public string Host => $"{_settings.Host}:{_settings.Port}";
    public string Username => _settings.Username;

    public ObservableCollection<DatabaseItem> Databases { get; } = new();
    public ObservableCollection<ResultRow> Results { get; } = new();
    public ObservableCollection<string> Columns { get; } = new();
    public ObservableCollection<LogRow> Logs { get; } = new();
    private readonly List<DatabaseLogEntry> _rawLogs = new();

    public int Threads
    {
        get => _threads;
        set
        {
            var clamped = Math.Clamp(value, WorkspaceDefaults.MinThreads, WorkspaceDefaults.MaxThreads);
            if (SetProperty(ref _threads, clamped))
                _session.Threads = clamped;
        }
    }

    public bool IsShowingQuery   => ActivePane == WorkspacePane.Query;
    public bool IsShowingResults => ActivePane == WorkspacePane.Results;
    public bool IsShowingLog     => ActivePane == WorkspacePane.Log;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        var selected = GetSelectedDatabases();
        if (selected.Count == 0 || string.IsNullOrWhiteSpace(Query)) return;

        foreach (var db in Databases)
            db.Reset();
        Logs.Clear();
        _rawLogs.Clear();
        LogsFilter.Reset();

        var outcome = await _session.RunAsync(Query, selected);

        var msg = outcome.Message is null ? outcome.Kind.ToString() : $"{outcome.Kind} — {outcome.Message}";
        RunStatus = $"Run: {msg}";
        _rawResults = _session.LastResults;
        RebuildResults();
        if (_rawResults.Count > 0)
            ActivePane = WorkspacePane.Results;
        else if (Logs.Count > 0)
            ActivePane = WorkspacePane.Log;
    }

    private bool CanRun() =>
        !IsRunning && Databases.Any(d => d.IsSelected) && !string.IsNullOrWhiteSpace(Query);

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => _session.Cancel();

    private bool CanCancel() => IsRunning;

    [RelayCommand]
    private void Disconnect() => Disconnected?.Invoke();

    [RelayCommand]
    private void SelectAll() => ToggleAll(true);

    [RelayCommand]
    private void SelectNone() => ToggleAll(false);

    [RelayCommand(CanExecute = nameof(CanExportCsv))]
    private async Task ExportCsvAsync()
    {
        if (Results.Count == 0) return;

        var fileName = string.Format(WorkspaceDefaults.CsvFileNamePattern, DateTime.Now);
        var path = Path.Combine(Directory.GetCurrentDirectory(), fileName);

        try
        {
            var rows = GetRowsForActiveFilter();
            await using var stream = File.Create(path);
            await CsvExporter.WriteAsync(stream, rows);
            RunStatus = $"Exported {rows.Count} row(s) → {path}";
        }
        catch (Exception ex)
        {
            RunStatus = $"Export failed: {ex.Message}";
        }
    }

    private bool CanExportCsv() => Results.Count > 0;

    [RelayCommand]
    private void ToggleView() => ActivePane = ActivePane switch
    {
        WorkspacePane.Query   => WorkspacePane.Results,
        WorkspacePane.Results => WorkspacePane.Log,
        _                     => WorkspacePane.Query
    };

    [RelayCommand]
    private void ShowQuery() => ActivePane = WorkspacePane.Query;

    [RelayCommand]
    private void ShowResults() => ActivePane = WorkspacePane.Results;

    [RelayCommand]
    private void ShowLog() => ActivePane = WorkspacePane.Log;

    [RelayCommand]
    private void ClearLog()
    {
        Logs.Clear();
        _rawLogs.Clear();
        LogsFilter.Reset();
    }

    private void AppendLog(DatabaseLogEntry entry)
    {
        LogsFilter.RegisterValue(entry.DatabaseName);

        _rawLogs.Add(entry);
        var row = new LogRow(entry);
        row.IsVisible = LogsFilter.Matches(row.Database);
        Logs.Add(row);
    }

    private void RefreshLogVisibility()
    {
        foreach (var row in Logs)
            row.IsVisible = LogsFilter.Matches(row.Database);
    }

    [RelayCommand(CanExecute = nameof(CanExportLog))]
    private async Task ExportLogAsync()
    {
        if (_rawLogs.Count == 0) return;

        var suggested = string.Format(WorkspaceDefaults.LogFileNamePattern, DateTime.Now);
        var path = await _fileDialogs.SaveFileAsync("Save log", suggested, "log", "Log file");
        if (path is null) return;

        try
        {
            await using var stream = File.Create(path);
            await LogExporter.WriteAsync(stream, _rawLogs);
            RunStatus = $"Exported {_rawLogs.Count} log entr(ies) → {path}";
        }
        catch (Exception ex)
        {
            RunStatus = $"Log export failed: {ex.Message}";
        }
    }

    private bool CanExportLog() => _rawLogs.Count > 0;

    private IReadOnlyList<string> GetSelectedDatabases() =>
        Databases.Where(d => d.IsSelected).Select(d => d.Name).ToList();

    private IReadOnlyList<DatabaseRow> GetRowsForActiveFilter() =>
        _rawResults.Where(r => ResultsFilter.Matches(r.Database)).ToList();

    private void RebuildResults()
    {
        var aggregated = ResultsAggregator.Aggregate(_rawResults);
        Columns.Clear();
        foreach (var c in aggregated.Columns) Columns.Add(c);

        ResultsFilter.Reset();
        ResultsFilter.RegisterValues(_rawResults.Select(r => r.Database).Distinct().OrderBy(x => x));

        // Apply filter explicitly: Reset() may not raise PropertyChanged if Selected was already "all".
        ApplyResultFilter();
    }

    private void ApplyResultFilter()
    {
        var aggregated = ResultsAggregator.Aggregate(GetRowsForActiveFilter());
        Results.Clear();
        foreach (var row in aggregated.Rows)
            Results.Add(new ResultRow(aggregated.Columns, row));
    }

    private void OnStatusChanged(DatabaseStatus status)
    {
        var item = Databases.FirstOrDefault(d => d.Name == status.DatabaseName);
        item?.ApplyStatus(status);
        if (IsRunning) RefreshRunStatus();
    }

    private void RefreshRunStatus()
    {
        var targeted = Databases.Count(d => d.IsSelected);
        var done = Databases.Count(d => d.IsSelected && d.IsTerminal);
        RunStatus = $"Running ({done}/{targeted})";
    }

    private void OnSelectionChanged()
    {
        RefreshSelectionSummary();
        RunCommand.NotifyCanExecuteChanged();
    }

    private void RefreshSelectionSummary()
    {
        var sel = Databases.Count(d => d.IsSelected);
        SelectionSummary = $"{sel} / {Databases.Count} selected";
    }

    private void ToggleAll(bool value)
    {
        foreach (var d in Databases.Where(d => d.IsVisible))
            d.IsSelected = value;
    }

}
