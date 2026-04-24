using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ForEachDb.Console.ViewModels;
using ForEachDbQueries;

namespace ForEachDb.Console.Views;

public partial class ConnectionView : UserControl
{
    public ConnectionView()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
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

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Q && (e.KeyModifiers & KeyModifiers.Control) != 0)
        {
            Quit();
            e.Handled = true;
        }
    }

    private void OnQuit(object? sender, RoutedEventArgs e) => Quit();

    private static void Quit()
    {
        var lifetime = Application.Current?.ApplicationLifetime as IControlledApplicationLifetime;
        lifetime?.Shutdown();
    }
}
