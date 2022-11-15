using Dapper;

namespace ForEachDbQueries;

public interface IDatabaseFinder
{
    IDatabaseFinder IgnoreTemplateDb();
    IDatabaseFinder IgnorePostgresDb();
    IDatabaseFinder IgnoreDatabase(string database);
    IDatabaseFinder OrderByName(bool ascending);
    IDatabaseFinder IncludeUnconnectableDatabases();
    SqlBuilder.Template Query();
}