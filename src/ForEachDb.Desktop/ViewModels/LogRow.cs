using CommunityToolkit.Mvvm.ComponentModel;
using ForEachDbQueries;

namespace ForEachDb.Desktop.ViewModels;

public sealed partial class LogRow : ObservableObject
{
    [ObservableProperty]
    private bool _isVisible = true;

    public LogRow(DatabaseLogEntry entry)
    {
        Timestamp = entry.Timestamp.LocalDateTime.ToString("HH:mm:ss");
        Database = entry.DatabaseName;
        Level = entry.Level.ToString().ToUpperInvariant();
        Message = entry.Message;

        IsInfo    = entry.Level == DatabaseLogLevel.Info;
        IsNotice  = entry.Level == DatabaseLogLevel.Notice;
        IsWarning = entry.Level == DatabaseLogLevel.Warning;
        IsError   = entry.Level == DatabaseLogLevel.Error;
    }

    public string Timestamp { get; }
    public string Database { get; }
    public string Level { get; }
    public string Message { get; }

    public bool IsInfo    { get; }
    public bool IsNotice  { get; }
    public bool IsWarning { get; }
    public bool IsError   { get; }
}
