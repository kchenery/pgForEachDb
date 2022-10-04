using Dapper;

namespace ForEachDbQueries;

public class DatabaseFinder : IDatabaseFinder
{
    private readonly SqlBuilder _sqlBuilder = new();
    private readonly string _queryTemplate = "SELECT datname FROM pg_database /**where**/ /**orderby**/";
    private bool _ignoreUnconnectableDbs = true; 

    private int _dbNameCount = 0;

    public IDatabaseFinder IgnoreTemplateDb()
    {
        _sqlBuilder.Where("datistemplate = false");
        return this;
    }

    public IDatabaseFinder IgnorePostgresDb()
    {
        _sqlBuilder.Where("datname != @postgres", new { postgres = "postgres" });
        return this;
    }

    public IDatabaseFinder IgnoreDatabase(string database)
    {
        _dbNameCount++;
        var paramName = $"@database{_dbNameCount}";
        
        var param = new DynamicParameters();
        param.Add(paramName, database);

        _sqlBuilder.Where($"datname != {paramName}", param);
        return this;
    }

    public IDatabaseFinder OrderByName(bool ascending = true)
    {
        _sqlBuilder.OrderBy($"datname {(ascending ? "ASC" : "DESC")}");
        return this;
    }

    public IDatabaseFinder IncludeUnconnectableDatabases()
    {
        _ignoreUnconnectableDbs = false;
        return this;
    }

    public SqlBuilder.Template Query()
    {
        if (_ignoreUnconnectableDbs)
        {
            _sqlBuilder.Where("datallowconn = true");
        }
        
        return _sqlBuilder.AddTemplate(_queryTemplate);
    }
}