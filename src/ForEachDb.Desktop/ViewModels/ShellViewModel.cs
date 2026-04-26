using CommunityToolkit.Mvvm.ComponentModel;
using ForEachDb.Desktop.Services;
using ForEachDbQueries;

namespace ForEachDb.Desktop.ViewModels;

public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialogs;
    private readonly RecipeStore _recipes = new();

    [ObservableProperty]
    private ViewModelBase _current;

    public ShellViewModel(IFileDialogService fileDialogs)
    {
        _fileDialogs = fileDialogs;
        var connection = new ConnectionViewModel(_recipes);
        connection.Connected += OnConnected;
        _current = connection;
    }

    private void OnConnected(ConnectionSettings settings, IReadOnlyList<DiscoveredDatabase> databases, Recipe? recipe)
    {
        var workspace = new WorkspaceViewModel(settings, databases, recipe, _recipes, _fileDialogs);
        workspace.Disconnected += OnDisconnect;
        Current = workspace;
    }

    private void OnDisconnect()
    {
        var connection = new ConnectionViewModel(_recipes);
        connection.Connected += OnConnected;
        Current = connection;
    }
}
