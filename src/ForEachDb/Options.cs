using System.Runtime.InteropServices;

namespace ForEachDb;
using CommandLine;
    
public class Options
{
    [Option('h', "host", HelpText = "Hostname to connect to.", Default = "localhost")]
    public string? HostName { get; set; }
    
    [Option('d', "database", HelpText = "Database to connect to.", Default = "postgres")]
    public string? Database { get; set; } 
    
    [Option('u', "username", HelpText = "Username for the connection", Default = "postgres")]
    public string? Username { get; set; } 
    
    [Option('p', "password", Required = true, HelpText = "Password for the connection")]
    public string? Password { get; set; }
    
    [Option("port", HelpText = "Password for the connection", Default = 5432)]
    public int Port { get; set; }

    [Option('q', "query", Required = true, HelpText = "Query to run against each database")]
    public string? Query { get; set; }
}