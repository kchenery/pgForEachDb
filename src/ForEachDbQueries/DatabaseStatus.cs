namespace ForEachDbQueries;

public enum DatabaseRunState
{
    Pending,
    Running,
    Succeeded,
    Failed,
    Cancelled
}

public sealed record DatabaseStatus(
    string DatabaseName,
    DatabaseRunState State,
    string? ErrorMessage = null,
    TimeSpan? Duration = null,
    int? RowCount = null);
