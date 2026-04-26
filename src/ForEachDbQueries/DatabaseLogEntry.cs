namespace ForEachDbQueries;

public enum DatabaseLogLevel
{
    Info,
    Notice,
    Warning,
    Error
}

public sealed record DatabaseLogEntry(
    string DatabaseName,
    DateTimeOffset Timestamp,
    DatabaseLogLevel Level,
    string Message);
