namespace ForEachDbQueries;

public sealed record Recipe(
    string Name,
    ConnectionSettings Connection,
    IReadOnlyList<string> SelectedDatabases,
    string Query,
    int Threads);
