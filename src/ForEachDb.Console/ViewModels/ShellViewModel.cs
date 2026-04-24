using ForEachDbQueries;

namespace ForEachDb.Console.ViewModels;

public sealed class ShellViewModel : ViewModelBase
{
    private ViewModelBase _current;
    private readonly RecipeStore _recipes = new();

    public ShellViewModel()
    {
        var connection = new ConnectionViewModel(_recipes);
        connection.Connected += OnConnected;
        _current = connection;
    }

    public ViewModelBase Current
    {
        get => _current;
        private set => SetProperty(ref _current, value);
    }

    private void OnConnected(ConnectionSettings settings, IReadOnlyList<string> databases, Recipe? recipe)
    {
        var workspace = new WorkspaceViewModel(settings, databases, recipe, _recipes);
        workspace.Disconnect += OnDisconnect;
        Current = workspace;
    }

    private void OnDisconnect()
    {
        var connection = new ConnectionViewModel(_recipes);
        connection.Connected += OnConnected;
        Current = connection;
    }
}
