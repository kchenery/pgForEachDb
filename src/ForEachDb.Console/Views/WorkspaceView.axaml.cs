using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ForEachDb.Console.ViewModels;

namespace ForEachDb.Console.Views;

public partial class WorkspaceView : UserControl
{
    private DataGrid? _grid;
    private WorkspaceViewModel? _vm;

    public WorkspaceView()
    {
        InitializeComponent();
        _grid = this.FindControl<DataGrid>("ResultsGrid");
        DataContextChanged += OnDataContextChanged;
        KeyDown += OnKeyDown;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
            _vm.Columns.CollectionChanged -= OnColumnsChanged;

        _vm = DataContext as WorkspaceViewModel;

        if (_vm is not null)
        {
            _vm.Columns.CollectionChanged += OnColumnsChanged;
            RebuildColumns();
        }
    }

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildColumns();

    private void RebuildColumns()
    {
        if (_grid is null || _vm is null) return;
        _grid.Columns.Clear();

        for (var i = 0; i < _vm.Columns.Count; i++)
        {
            _grid.Columns.Add(new DataGridTextColumn
            {
                Header = _vm.Columns[i],
                Binding = new Binding($"Cells[{i}].Value"),
                IsReadOnly = true,
                CanUserResize = true,
                CanUserReorder = true,
                CanUserSort = true,
                Width = DataGridLength.Auto
            });
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (e.KeyModifiers & KeyModifiers.Control) != 0)
        {
            if (_vm?.RunCommand.CanExecute(null) == true)
                _vm.RunCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Q && (e.KeyModifiers & KeyModifiers.Control) != 0)
        {
            _vm?.Quit();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F2 && _vm is not null)
        {
            _vm.ToggleViewCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if ((e.KeyModifiers & KeyModifiers.Alt) != 0 && e.Key is Key.Left or Key.Right)
        {
            var grid = this.FindControl<Grid>("BodyGrid");
            if (grid is { ColumnDefinitions.Count: > 0 })
            {
                var column = grid.ColumnDefinitions[0];
                var delta = e.Key == Key.Left ? -2 : 2;
                var current = column.Width.IsAbsolute ? column.Width.Value : 28;
                var min = column.MinWidth > 0 ? column.MinWidth : 8;
                var next = Math.Max(min, current + delta);
                column.Width = new GridLength(next, GridUnitType.Pixel);
                e.Handled = true;
            }
        }
    }

    private void OnDatabaseActivated(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: DatabaseItem item })
            item.Toggle();
    }

    private void OnDatabaseListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter &&
            sender is ListBox { SelectedItem: DatabaseItem item })
        {
            item.Toggle();
            e.Handled = true;
        }
    }

    private void OnQuit(object? sender, RoutedEventArgs e) => _vm?.Quit();
}
