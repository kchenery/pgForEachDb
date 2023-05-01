using System.Data;
using Dapper;

namespace ForEachDbQueries.DapperExtensions;

public static class DapperExtensions
{
    public static Task<IEnumerable<T>> QueryAsync<T>(this IDbConnection? cnn, IDatabaseFinder dbFinder,
        IDbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null) =>
        cnn.QueryAsync<T>(dbFinder.Query().RawSql, dbFinder.Query().Parameters, transaction, commandTimeout,
            commandType);
}