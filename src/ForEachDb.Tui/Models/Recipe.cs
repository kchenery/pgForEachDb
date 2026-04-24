namespace ForEachDb.Tui.Models;

public sealed record Recipe(
    string Name,
    ConnectionSettings Connection,
    IReadOnlyList<string> SelectedDatabases,
    string Query,
    int Threads);
