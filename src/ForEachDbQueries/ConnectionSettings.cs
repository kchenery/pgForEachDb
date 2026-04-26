namespace ForEachDbQueries;

public sealed record ConnectionSettings(
    string Host,
    int Port,
    string Database,
    string Username,
    string Password);
