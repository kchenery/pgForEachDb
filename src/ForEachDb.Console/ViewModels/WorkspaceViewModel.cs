using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using ForEachDbQueries;

namespace ForEachDb.Console.ViewModels;

public sealed class WorkspaceViewModel : ViewModelBase
{
    private readonly RunSession _session;
    private readonly RecipeStore _recipes;
    private readonly ConnectionSettings _settings;

    private string _query;
    private int _threads = 4;
    private string _runStatus = "Idle.";
    private bool _isRunning;
    private string? _dbFilter;
    private string _saveRecipeName = string.Empty;
    private bool _isSavingRecipe;
    private WorkspacePane _activePane = WorkspacePane.Query;

    public WorkspaceViewModel(
        ConnectionSettings settings,
        IReadOnlyList<string> databases,
        Recipe? recipe,
        RecipeStore recipes)
    {
        _settings = settings;
        _recipes = recipes;
        _session = new RunSession(settings, databases);
        _session.StatusChanged += OnStatusChanged;
        _session.RunStarted += OnRunStarted;
        _session.RunCompleted += OnRunCompleted;
        _session.LogEntryAppended += OnLogEntryAppended;

        _query = recipe?.Query ?? string.Empty;
        _threads = recipe?.Threads ?? 4;
        _session.Threads = _threads;

        var preselected = new HashSet<string>(
            recipe?.SelectedDatabases ?? databases,
            StringComparer.Ordinal);

        foreach (var name in databases)
            Databases.Add(new DatabaseItem(name, preselected.Contains(name), OnSelectionChanged));
        RefreshSelectionSummary();

        RunCommand = new AsyncRelayCommand(RunAsync, () => !IsRunning && Databases.Any(d => d.IsSelected) && !string.IsNullOrWhiteSpace(Query));
        CancelCommand = new RelayCommand(() => _session.Cancel(), () => IsRunning);
        DisconnectCommand = new RelayCommand(() => Disconnect?.Invoke());
        SelectAllCommand = new RelayCommand(() => ToggleAll(true));
        SelectNoneCommand = new RelayCommand(() => ToggleAll(false));
        ExportCsvCommand = new AsyncRelayCommand(ExportCsvAsync, () => Results.Count > 0);
        BeginSaveRecipeCommand = new RelayCommand(() => { SaveRecipeName = recipe?.Name ?? string.Empty; IsSavingRecipe = true; });
        ConfirmSaveRecipeCommand = new RelayCommand(ConfirmSaveRecipe, () => !string.IsNullOrWhiteSpace(SaveRecipeName));
        CancelSaveRecipeCommand = new RelayCommand(() => IsSavingRecipe = false);
        ToggleViewCommand = new RelayCommand(CyclePane);
        ShowQueryCommand   = new RelayCommand(() => ActivePane = WorkspacePane.Query);
        ShowResultsCommand = new RelayCommand(() => ActivePane = WorkspacePane.Results);
        ShowLogCommand     = new RelayCommand(() => ActivePane = WorkspacePane.Log);
        ClearLogCommand    = new RelayCommand(() => Logs.Clear());
    }

    public event Action? Disconnect;

    public string Host => $"{_settings.Host}:{_settings.Port}";
    public string Username => _settings.Username;

    public ObservableCollection<DatabaseItem> Databases { get; } = new();
    public ObservableCollection<ResultRow> Results { get; } = new();
    public ObservableCollection<string> Columns { get; } = new();
    public ObservableCollection<string> DatabaseFilters { get; } = new() { "(all databases)" };
    public ObservableCollection<LogRow> Logs { get; } = new();

    private string _selectionSummary = string.Empty;
    public string SelectionSummary { get => _selectionSummary; private set => SetProperty(ref _selectionSummary, value); }

    public string Query
    {
        get => _query;
        set
        {
            if (SetProperty(ref _query, value))
                ((AsyncRelayCommand)RunCommand).NotifyCanExecuteChanged();
        }
    }

    public int Threads
    {
        get => _threads;
        set
        {
            var clamped = Math.Clamp(value, 1, 64);
            if (SetProperty(ref _threads, clamped))
                _session.Threads = clamped;
        }
    }

    public string RunStatus { get => _runStatus; private set => SetProperty(ref _runStatus, value); }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                ((AsyncRelayCommand)RunCommand).NotifyCanExecuteChanged();
                ((RelayCommand)CancelCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public string? DatabaseFilter
    {
        get => _dbFilter;
        set
        {
            if (SetProperty(ref _dbFilter, value))
                ApplyFilter();
        }
    }

    public bool IsSavingRecipe { get => _isSavingRecipe; set => SetProperty(ref _isSavingRecipe, value); }

    public string SaveRecipeName
    {
        get => _saveRecipeName;
        set
        {
            if (SetProperty(ref _saveRecipeName, value))
                ((RelayCommand)ConfirmSaveRecipeCommand).NotifyCanExecuteChanged();
        }
    }

    public ICommand RunCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand BeginSaveRecipeCommand { get; }
    public ICommand ConfirmSaveRecipeCommand { get; }
    public ICommand CancelSaveRecipeCommand { get; }
    public ICommand ToggleViewCommand { get; }
    public ICommand ShowQueryCommand { get; }
    public ICommand ShowResultsCommand { get; }
    public ICommand ShowLogCommand { get; }
    public ICommand ClearLogCommand { get; }

    public WorkspacePane ActivePane
    {
        get => _activePane;
        set
        {
            if (SetProperty(ref _activePane, value))
            {
                OnPropertyChanged(nameof(IsShowingQuery));
                OnPropertyChanged(nameof(IsShowingResults));
                OnPropertyChanged(nameof(IsShowingLog));
            }
        }
    }

    public bool IsShowingQuery   => _activePane == WorkspacePane.Query;
    public bool IsShowingResults => _activePane == WorkspacePane.Results;
    public bool IsShowingLog     => _activePane == WorkspacePane.Log;

    private void CyclePane() => ActivePane = _activePane switch
    {
        WorkspacePane.Query   => WorkspacePane.Results,
        WorkspacePane.Results => WorkspacePane.Log,
        _                     => WorkspacePane.Query
    };

    private IReadOnlyList<DatabaseRow> _rawResults = Array.Empty<DatabaseRow>();

    private async Task RunAsync()
    {
        var selected = Databases.Where(d => d.IsSelected).Select(d => d.Name).ToList();
        if (selected.Count == 0 || string.IsNullOrWhiteSpace(Query)) return;

        foreach (var db in Databases)
            db.Reset();
        Logs.Clear();

        var outcome = await _session.RunAsync(Query, selected);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var msg = outcome.Message is null ? outcome.Kind.ToString() : $"{outcome.Kind} — {outcome.Message}";
            RunStatus = $"Run: {msg}";
            _rawResults = _session.LastResults;
            RebuildResults();
            if (_rawResults.Count > 0)
                ActivePane = WorkspacePane.Results;
            else if (Logs.Count > 0)
                ActivePane = WorkspacePane.Log;
        });
    }

    private void RebuildResults()
    {
        var aggregated = ResultsAggregator.Aggregate(_rawResults);
        Columns.Clear();
        foreach (var c in aggregated.Columns) Columns.Add(c);

        DatabaseFilters.Clear();
        DatabaseFilters.Add("(all databases)");
        foreach (var d in _rawResults.Select(r => r.Database).Distinct().OrderBy(x => x))
            DatabaseFilters.Add(d);

        _dbFilter = "(all databases)";
        OnPropertyChanged(nameof(DatabaseFilter));
        ApplyFilter();
        ((AsyncRelayCommand)ExportCsvCommand).NotifyCanExecuteChanged();
    }

    private void ApplyFilter()
    {
        var filtered = (DatabaseFilter is null or "(all databases)")
            ? _rawResults
            : _rawResults.Where(r => r.Database == DatabaseFilter).ToList();

        var aggregated = ResultsAggregator.Aggregate(filtered);
        Results.Clear();
        foreach (var row in aggregated.Rows)
            Results.Add(new ResultRow(aggregated.Columns, row));
    }

    private void OnRunStarted() => Dispatcher.UIThread.Post(() => { IsRunning = true; RefreshRunStatus(); });

    private void OnRunCompleted(RunOutcome outcome) => Dispatcher.UIThread.Post(() => IsRunning = false);

    private void OnStatusChanged(DatabaseStatus status) => Dispatcher.UIThread.Post(() =>
    {
        var item = Databases.FirstOrDefault(d => d.Name == status.DatabaseName);
        item?.ApplyStatus(status);
        if (IsRunning) RefreshRunStatus();
    });

    private void RefreshRunStatus()
    {
        var targeted = Databases.Count(d => d.IsSelected);
        var done = Databases.Count(d => d.IsSelected && d.IsTerminal);
        RunStatus = $"Running ({done}/{targeted})";
    }

    private void OnLogEntryAppended(DatabaseLogEntry entry) =>
        Dispatcher.UIThread.Post(() => Logs.Add(new LogRow(entry)));

    private void OnSelectionChanged()
    {
        RefreshSelectionSummary();
        ((AsyncRelayCommand)RunCommand).NotifyCanExecuteChanged();
    }

    private void RefreshSelectionSummary()
    {
        var sel = Databases.Count(d => d.IsSelected);
        SelectionSummary = $"{sel} / {Databases.Count} selected";
    }

    private void ToggleAll(bool value)
    {
        foreach (var d in Databases) d.IsSelected = value;
    }

    public void Quit()
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime
            as Avalonia.Controls.ApplicationLifetimes.IControlledApplicationLifetime;
        lifetime?.Shutdown();
    }

    private async Task ExportCsvAsync()
    {
        if (Results.Count == 0) return;
        var path = Path.Combine(
            Directory.GetCurrentDirectory(),
            $"pgForEachDb-{DateTime.Now:yyyyMMdd-HHmmss}.csv");

        try
        {
            var rows = (DatabaseFilter is null or "(all databases)")
                ? _rawResults
                : _rawResults.Where(r => r.Database == DatabaseFilter).ToList();

            await using var stream = File.Create(path);
            await CsvExporter.WriteAsync(stream, rows);
            RunStatus = $"Exported {rows.Count} row(s) → {path}";
        }
        catch (Exception ex)
        {
            RunStatus = $"Export failed: {ex.Message}";
        }
    }

    private void ConfirmSaveRecipe()
    {
        var name = SaveRecipeName.Trim();
        if (string.IsNullOrEmpty(name)) return;

        var selected = Databases.Where(d => d.IsSelected).Select(d => d.Name).ToList();
        var recipe = new Recipe(
            Name: name,
            Connection: _settings with { Password = string.Empty },
            SelectedDatabases: selected,
            Query: Query,
            Threads: Threads);

        _recipes.Save(recipe);
        IsSavingRecipe = false;
        RunStatus = $"Saved recipe '{name}'.";
    }
}

public sealed class DatabaseItem : ViewModelBase
{
    private readonly Action _onToggled;
    private bool _isSelected;
    private string _state = "pending";

    public DatabaseItem(string name, bool isSelected, Action onToggled)
    {
        Name = name;
        _isSelected = isSelected;
        _onToggled = onToggled;
    }

    public string Name { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(Glyph));
                _onToggled();
            }
        }
    }

    public string Glyph => _isSelected ? "✓" : "·";

    public void Toggle() => IsSelected = !IsSelected;

    public string State { get => _state; private set => SetProperty(ref _state, value); }

    public DatabaseRunState RunState { get; private set; } = DatabaseRunState.Pending;

    public bool IsTerminal => RunState is DatabaseRunState.Succeeded or DatabaseRunState.Failed or DatabaseRunState.Cancelled;

    public void ApplyStatus(DatabaseStatus status)
    {
        RunState = status.State;
        State = status.State switch
        {
            DatabaseRunState.Running   => "▶ running",
            DatabaseRunState.Succeeded => "✓ done",
            DatabaseRunState.Failed    => "✗ failed",
            DatabaseRunState.Cancelled => "⊘ cancelled",
            _                          => status.State.ToString().ToLowerInvariant()
        };
    }

    public void Reset()
    {
        RunState = DatabaseRunState.Pending;
        State = "pending";
    }
}

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

public enum WorkspacePane { Query, Results, Log }

public sealed class LogRow
{
    public LogRow(DatabaseLogEntry entry)
    {
        Timestamp = entry.Timestamp.LocalDateTime.ToString("HH:mm:ss");
        Database = entry.DatabaseName;
        Level = entry.Level.ToString().ToUpperInvariant();
        Message = entry.Message;
    }

    public string Timestamp { get; }
    public string Database { get; }
    public string Level { get; }
    public string Message { get; }
}
