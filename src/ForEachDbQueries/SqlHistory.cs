namespace ForEachDbQueries;

/// <summary>
/// Per-session ring buffer of submitted SQL statements with up/down navigation.
/// Preserves the in-progress draft when the user navigates back past the newest entry.
/// </summary>
public sealed class SqlHistory
{
    private readonly int _capacity;
    private readonly List<string> _entries = new();
    private int _index = -1;
    private string _draft = string.Empty;

    public SqlHistory(int capacity = 100)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public int Count => _entries.Count;

    public IReadOnlyList<string> Snapshot() => _entries.ToList();

    public void Push(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _index = -1;
            return;
        }

        if (_entries.Count == 0 || !string.Equals(_entries[^1], query, StringComparison.Ordinal))
        {
            _entries.Add(query);
            if (_entries.Count > _capacity) _entries.RemoveAt(0);
        }

        _index = -1;
        _draft = string.Empty;
    }

    public string? Older(string currentText)
    {
        if (_entries.Count == 0) return null;

        if (_index == -1)
        {
            _draft = currentText;
            _index = _entries.Count - 1;
            return _entries[_index];
        }

        if (_index > 0)
        {
            _index--;
            return _entries[_index];
        }

        return null;
    }

    public string? Newer()
    {
        if (_index == -1) return null;

        if (_index < _entries.Count - 1)
        {
            _index++;
            return _entries[_index];
        }

        _index = -1;
        return _draft;
    }
}
