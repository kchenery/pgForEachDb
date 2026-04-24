using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ForEachDbQueries;
using ForEachDb.Console.Services;

namespace ForEachDb.Console.ViewModels;

public sealed class ConnectionViewModel : ViewModelBase
{
    private readonly RecipeStore _recipes;

    private string _host = "localhost";
    private string _port = "5432";
    private string _database = "postgres";
    private string _username = "postgres";
    private string _password = string.Empty;
    private bool _includePostgres;
    private bool _includeTemplate;
    private string _ignoreList = string.Empty;
    private string _status = string.Empty;
    private bool _isBusy;
    private Recipe? _loadedRecipe;

    public ConnectionViewModel(RecipeStore recipes)
    {
        _recipes = recipes;
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !IsBusy);
        LoadRecipeCommand = new RelayCommand<Recipe?>(ApplyRecipe);
        foreach (var recipe in _recipes.Load())
            Recipes.Add(recipe);
    }

    public event Action<ConnectionSettings, IReadOnlyList<string>, Recipe?>? Connected;

    public ObservableCollection<Recipe> Recipes { get; } = new();

    public string Host { get => _host; set => SetProperty(ref _host, value); }
    public string Port { get => _port; set => SetProperty(ref _port, value); }
    public string Database { get => _database; set => SetProperty(ref _database, value); }
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public bool IncludePostgres { get => _includePostgres; set => SetProperty(ref _includePostgres, value); }
    public bool IncludeTemplate { get => _includeTemplate; set => SetProperty(ref _includeTemplate, value); }
    public string IgnoreList { get => _ignoreList; set => SetProperty(ref _ignoreList, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
                ((AsyncRelayCommand)ConnectCommand).NotifyCanExecuteChanged();
        }
    }

    public ICommand ConnectCommand { get; }
    public ICommand LoadRecipeCommand { get; }

    private void ApplyRecipe(Recipe? recipe)
    {
        if (recipe is null) return;
        _loadedRecipe = recipe;
        Host = recipe.Connection.Host;
        Port = recipe.Connection.Port.ToString();
        Database = recipe.Connection.Database;
        Username = recipe.Connection.Username;
        IncludePostgres = recipe.Connection.IncludePostgresDb;
        IncludeTemplate = recipe.Connection.IncludeTemplateDb;
        IgnoreList = string.Join(", ", recipe.Connection.IgnoreDatabases);
        Status = $"Loaded recipe '{recipe.Name}'. Enter password and connect.";
    }

    private async Task ConnectAsync()
    {
        if (!int.TryParse(Port, out var port))
        {
            Status = "Port must be numeric.";
            return;
        }

        var ignores = IgnoreList
            .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var settings = new ConnectionSettings(
            Host, port, Database, Username, Password,
            IncludePostgres, IncludeTemplate, ignores);

        IsBusy = true;
        Status = $"Connecting to {Host}:{port}…";
        try
        {
            var databases = await DatabaseProbe.DiscoverAsync(settings);
            if (databases.Count == 0)
            {
                Status = "Connected, but no databases matched the filters.";
                return;
            }
            Status = $"Connected — found {databases.Count} database(s).";
            Connected?.Invoke(settings, databases, _loadedRecipe);
        }
        catch (Exception ex)
        {
            Status = $"Error: {ex.InnerException?.Message ?? ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
