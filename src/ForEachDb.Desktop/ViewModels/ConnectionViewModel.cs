using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForEachDbQueries;
using ForEachDb.Desktop.Services;

namespace ForEachDb.Desktop.ViewModels;

public sealed partial class ConnectionViewModel : ViewModelBase
{
    private readonly RecipeStore _recipes;
    private Recipe? _loadedRecipe;

    [ObservableProperty] private string _host = "localhost";
    [ObservableProperty] private string _port = "5432";
    [ObservableProperty] private string _database = "postgres";
    [ObservableProperty] private string _username = "postgres";
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _status = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private bool _isBusy;

    public ConnectionViewModel(RecipeStore recipes)
    {
        _recipes = recipes;
        foreach (var recipe in _recipes.Load())
            Recipes.Add(recipe);
    }

    public event Action<ConnectionSettings, IReadOnlyList<DiscoveredDatabase>, Recipe?>? Connected;

    public ObservableCollection<Recipe> Recipes { get; } = new();

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync()
    {
        if (!int.TryParse(Port, out var port))
        {
            Status = "Port must be numeric.";
            return;
        }

        var settings = new ConnectionSettings(Host, port, Database, Username, Password);

        IsBusy = true;
        Status = $"Connecting to {Host}:{port}…";
        try
        {
            var databases = await DatabaseProbe.DiscoverAsync(settings);
            if (databases.Count == 0)
            {
                Status = "Connected, but no databases were returned.";
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

    private bool CanConnect() => !IsBusy;

    [RelayCommand]
    private void LoadRecipe(Recipe? recipe)
    {
        if (recipe is null) return;
        _loadedRecipe = recipe;
        Host = recipe.Connection.Host;
        Port = recipe.Connection.Port.ToString();
        Database = recipe.Connection.Database;
        Username = recipe.Connection.Username;
        Status = $"Loaded recipe '{recipe.Name}'. Enter password and connect.";
    }
}
