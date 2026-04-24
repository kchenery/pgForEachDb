using ForEachDbQueries;
using Terminal.Gui;

namespace ForEachDb.Tui.Views;

public sealed class DatabaseListView : FrameView
{
    private static readonly string[] SpinnerFrames =
        ["\u2840", "\u2844", "\u2846", "\u2847", "\u2807", "\u2803", "\u2801", "\u2809", "\u2819", "\u2838", "\u2830", "\u2820"];

    private readonly ListView _list;
    private readonly IReadOnlyList<string> _databases;
    private readonly List<string> _display;
    private readonly Dictionary<string, DatabaseStatus> _statuses = new();
    private int _spinnerFrame;

    public event Action? SelectionChanged;
    public event Action<string>? RowActivated;

    public DatabaseListView(IReadOnlyList<string> databases) : base("Databases")
    {
        _databases = databases;
        _display = databases.Select(FormatPending).ToList();

        _list = new ListView(_display)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            AllowsMarking = true,
            AllowsMultipleSelection = true,
            CanFocus = true
        };

        for (var i = 0; i < databases.Count; i++)
            _list.Source.SetMark(i, true);

        _list.SelectedItemChanged += _ => SelectionChanged?.Invoke();
        _list.OpenSelectedItem += args =>
        {
            if (args.Item >= 0 && args.Item < _databases.Count)
                RowActivated?.Invoke(_databases[args.Item]);
        };

        Add(_list);
    }

    public IReadOnlyList<string> SelectedDatabases
    {
        get
        {
            var selected = new List<string>();
            for (var i = 0; i < _databases.Count; i++)
                if (_list.Source.IsMarked(i)) selected.Add(_databases[i]);
            return selected;
        }
    }

    public int SelectedCount
    {
        get
        {
            var count = 0;
            for (var i = 0; i < _databases.Count; i++)
                if (_list.Source.IsMarked(i)) count++;
            return count;
        }
    }

    public int TotalCount => _databases.Count;

    public string? SelectedRowDatabase
    {
        get
        {
            var i = _list.SelectedItem;
            return i >= 0 && i < _databases.Count ? _databases[i] : null;
        }
    }

    public IReadOnlyDictionary<string, DatabaseStatus> Statuses => _statuses;

    public void SelectAll()
    {
        for (var i = 0; i < _databases.Count; i++) _list.Source.SetMark(i, true);
        _list.SetNeedsDisplay();
        SelectionChanged?.Invoke();
    }

    public void SelectNone()
    {
        for (var i = 0; i < _databases.Count; i++) _list.Source.SetMark(i, false);
        _list.SetNeedsDisplay();
        SelectionChanged?.Invoke();
    }

    public void SetSelection(IEnumerable<string> names)
    {
        var wanted = new HashSet<string>(names, StringComparer.Ordinal);
        for (var i = 0; i < _databases.Count; i++)
            _list.Source.SetMark(i, wanted.Contains(_databases[i]));
        _list.SetNeedsDisplay();
        SelectionChanged?.Invoke();
    }

    public void ResetStatuses()
    {
        _statuses.Clear();
        for (var i = 0; i < _databases.Count; i++)
            _display[i] = FormatPending(_databases[i]);
        _list.SetNeedsDisplay();
    }

    public void ApplyStatus(DatabaseStatus status)
    {
        _statuses[status.DatabaseName] = status;
        var index = IndexOf(status.DatabaseName);
        if (index < 0) return;

        _display[index] = Format(status);
        _list.SetNeedsDisplay();
    }

    public void TickSpinner()
    {
        _spinnerFrame = (_spinnerFrame + 1) % SpinnerFrames.Length;
        var changed = false;
        foreach (var (name, status) in _statuses)
        {
            if (status.State != DatabaseRunState.Running) continue;
            var index = IndexOf(name);
            if (index < 0) continue;
            _display[index] = Format(status);
            changed = true;
        }
        if (changed) _list.SetNeedsDisplay();
    }

    private int IndexOf(string name)
    {
        for (var i = 0; i < _databases.Count; i++)
            if (_databases[i] == name) return i;
        return -1;
    }

    private string Format(DatabaseStatus status)
    {
        var name = status.DatabaseName;
        return status.State switch
        {
            DatabaseRunState.Pending   => FormatPending(name),
            DatabaseRunState.Running   => $"{SpinnerFrames[_spinnerFrame]} {name}",
            DatabaseRunState.Succeeded => $"\u2714 {name}{FormatDuration(status)}",
            DatabaseRunState.Failed    => $"\u2718 {name}",
            DatabaseRunState.Cancelled => $"\u25d0 {name}",
            _ => name
        };
    }

    private static string FormatPending(string name) => $"\u25cb {name}";

    private static string FormatDuration(DatabaseStatus status) =>
        status.Duration is { } d ? $"  {d.TotalSeconds:F1}s" : string.Empty;
}
