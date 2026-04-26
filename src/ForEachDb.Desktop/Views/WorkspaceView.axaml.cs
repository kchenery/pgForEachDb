using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using ForEachDb.Desktop.ViewModels;
using TextMateSharp.Grammars;

namespace ForEachDb.Desktop.Views;

public partial class WorkspaceView : UserControl
{
    private DataGrid? _grid;
    private TextEditor? _editor;
    private TextMate.Installation? _textMate;
    private WorkspaceViewModel? _vm;

    public WorkspaceView()
    {
        InitializeComponent();
        _grid = this.FindControl<DataGrid>("ResultsGrid");
        _editor = this.FindControl<TextEditor>("QueryEditor");

        if (_editor is not null)
        {
            var registry = new RegistryOptions(ThemeName.DarkPlus);
            _textMate = _editor.InstallTextMate(registry);
            var sql = registry.GetLanguageByExtension(".sql");
            if (sql is not null)
                _textMate.SetGrammar(registry.GetScopeByLanguageId(sql.Id));
        }

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

        if (e.Key == Key.F2 && _vm is not null)
        {
            _vm.ToggleViewCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnDatabaseListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space &&
            sender is ListBox { SelectedItem: DatabaseItem item })
        {
            item.IsSelected = !item.IsSelected;
            e.Handled = true;
        }
    }
}
