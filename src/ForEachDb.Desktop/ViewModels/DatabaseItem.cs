using CommunityToolkit.Mvvm.ComponentModel;
using ForEachDbQueries;

namespace ForEachDb.Desktop.ViewModels;

public sealed partial class DatabaseItem : ObservableObject
{
    private readonly Action _onToggled;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPending))]
    [NotifyPropertyChangedFor(nameof(IsRunning))]
    [NotifyPropertyChangedFor(nameof(IsSucceeded))]
    [NotifyPropertyChangedFor(nameof(IsFailed))]
    [NotifyPropertyChangedFor(nameof(IsCancelled))]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    [NotifyPropertyChangedFor(nameof(IsTerminal))]
    [NotifyPropertyChangedFor(nameof(BadgeText))]
    private DatabaseRunState _runState = DatabaseRunState.Pending;

    public DatabaseItem(string name, bool isTemplate, bool isSelected, Action onToggled)
    {
        Name = name;
        IsTemplate = isTemplate;
        _isSelected = isSelected;
        _onToggled = onToggled;
    }

    public string Name { get; }
    public bool IsTemplate { get; }

    partial void OnIsSelectedChanged(bool value) => _onToggled();

    public bool IsPending   => RunState == DatabaseRunState.Pending;
    public bool IsRunning   => RunState == DatabaseRunState.Running;
    public bool IsSucceeded => RunState == DatabaseRunState.Succeeded;
    public bool IsFailed    => RunState == DatabaseRunState.Failed;
    public bool IsCancelled => RunState == DatabaseRunState.Cancelled;

    public bool HasStatus => RunState != DatabaseRunState.Pending;

    public bool IsTerminal => RunState
        is DatabaseRunState.Succeeded
        or DatabaseRunState.Failed
        or DatabaseRunState.Cancelled;

    public string BadgeText => RunState switch
    {
        DatabaseRunState.Running   => "running",
        DatabaseRunState.Succeeded => "done",
        DatabaseRunState.Failed    => "failed",
        DatabaseRunState.Cancelled => "cancelled",
        _                          => "pending"
    };

    public void ApplyStatus(DatabaseStatus status) => RunState = status.State;

    public void Reset() => RunState = DatabaseRunState.Pending;
}
