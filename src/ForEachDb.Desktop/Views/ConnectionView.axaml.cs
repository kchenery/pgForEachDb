using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ForEachDb.Desktop.ViewModels;
using ForEachDbQueries;

namespace ForEachDb.Desktop.Views;

public partial class ConnectionView : UserControl
{
    public ConnectionView()
    {
        InitializeComponent();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnRecipeActivated(object? sender, TappedEventArgs e)
    {
        if (DataContext is ConnectionViewModel vm &&
            sender is ListBox { SelectedItem: Recipe recipe })
        {
            vm.LoadRecipeCommand.Execute(recipe);
        }
    }
}
