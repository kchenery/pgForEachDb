using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ForEachDb.Desktop.Services;
using ForEachDb.Desktop.ViewModels;

namespace ForEachDb.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new ShellViewModel(new AvaloniaFileDialogService(this));
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
