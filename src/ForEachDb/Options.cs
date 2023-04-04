using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace ForEachDb;
using CommandLine;
    
[UsedImplicitly]
public class Options
{
    [Option('q', "query", Required = true, HelpText = "Query to run against each database")]
    public string? Query { get; set; }
    
    [Option('h', "host", HelpText = "Hostname to connect to.", Default = "localhost")]
    public string? HostName { get; set; }
    
    [Option('d', "database", HelpText = "Database to connect to.", Default = "postgres")]
    public string? Database { get; set; } 
    
    [Option('u', "username", HelpText = "Username for the connection", Default = "postgres")]
    public string? Username { get; set; } 
    
    [Option('p', "password", HelpText = "Password for the connection")]
    public string? Password { get; set; }
    
    [Option("port", HelpText = "Password for the connection", Default = 5432)]
    public int Port { get; set; }
    
    [Option("ignore", HelpText = "List of databases that should be ignored. E.g: --ignore foo bar baz")]
    public IEnumerable<string>? IgnoreDatabases { get; set; }
    
    [Option("include-postgres-db", HelpText = "Flag to include the postgres database")]
    public bool IncludePostgresDb { get; set; }
    
    [Option("include-template-db", HelpText = "Flag to include the postgres database")]
    public bool IncludeTemplateDb { get; set; }
}