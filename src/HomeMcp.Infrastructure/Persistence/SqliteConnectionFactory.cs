using System.Data;
using Microsoft.Data.Sqlite;

namespace HomeMcp.Infrastructure.Persistence;

public sealed class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString) =>
        _connectionString = connectionString;

    public IDbConnection CreateConnection() => new SqliteConnection(_connectionString);
}
