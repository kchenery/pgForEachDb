using Dapper;

namespace ForEachDbQueries;

public interface IDatabaseFinder
{
    IDatabaseFinder IgnoreTemplateDb();
    IDatabaseFinder IgnorePostgresDb();
    IDatabaseFinder IgnoreDatabase(string database);
    IDatabaseFinder IgnoreDatabases(IEnumerable<string> databases);
    IDatabaseFinder OrderByName(bool ascending);
    IDatabaseFinder IncludeUnconnectableDatabases();
    IDatabaseFinder IncludeRdsAdmin();
    SqlBuilder.Template Query();
}