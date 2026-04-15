using System.Data;
using Dapper;

namespace ForEachDbQueries.DapperExtensions;

public static class DapperExtensions
{
    public static Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection? cnn, IDatabaseFinder dbFinder,
        IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        if (cnn is null)
        {
            throw new ArgumentNullException(nameof(cnn));
        }

        var query = dbFinder.Query();
        return cnn.QueryAsync<T>(query.RawSql, query.Parameters, transaction, commandTimeout,
            commandType);
    }
}