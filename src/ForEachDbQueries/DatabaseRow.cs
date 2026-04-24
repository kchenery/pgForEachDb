namespace ForEachDbQueries;

public sealed record DatabaseRow(
    string Database,
    IReadOnlyDictionary<string, object?> Values);
