namespace ForEachDbQueries;

public enum DatabaseRunState
{
    Running,
    Succeeded,
    Failed
}

public sealed record DatabaseStatus(string DatabaseName, DatabaseRunState State, string? ErrorMessage = null);
