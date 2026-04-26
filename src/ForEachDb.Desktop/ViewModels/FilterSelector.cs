using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ForEachDb.Desktop.ViewModels;

/// <summary>
/// A "select one of N values, with an 'all' sentinel" filter, used by both the results and
/// log panes to filter by source database. The view binds <see cref="Values"/> to a ComboBox
/// and <see cref="Selected"/> to its SelectedItem; the VM listens to <see cref="ObservableObject.PropertyChanged"/>
/// for <see cref="Selected"/> and reacts.
/// </summary>
public sealed partial class FilterSelector : ObservableObject
{
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);

    [ObservableProperty]
    private string? _selected = WorkspaceDefaults.AllDatabasesFilter;

    public ObservableCollection<string> Values { get; } = new() { WorkspaceDefaults.AllDatabasesFilter };

    public bool Matches(string candidate) =>
        Selected is null
        || Selected == WorkspaceDefaults.AllDatabasesFilter
        || Selected == candidate;

    public void RegisterValue(string value)
    {
        if (_seen.Add(value))
            Values.Add(value);
    }

    public void RegisterValues(IEnumerable<string> values)
    {
        foreach (var v in values) RegisterValue(v);
    }

    public void Reset()
    {
        _seen.Clear();
        Values.Clear();
        Values.Add(WorkspaceDefaults.AllDatabasesFilter);
        Selected = WorkspaceDefaults.AllDatabasesFilter;
    }
}
