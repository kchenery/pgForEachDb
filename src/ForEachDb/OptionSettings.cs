namespace ForEachDb;

public class OptionSettings
{
    public string ConnectionString { get; set; }
    public string Query { get; set; }
    public List<string> IgnoreDatabases { get; set; }
}