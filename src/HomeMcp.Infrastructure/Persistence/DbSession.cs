using System.Data;

namespace HomeMcp.Infrastructure.Persistence;

public sealed class DbSession : IDisposable
{
    private IDbTransaction? _transaction;

    public DbSession(IDbConnectionFactory factory)
    {
        Connection = factory.CreateConnection();
        Connection.Open();
    }

    public IDbConnection Connection { get; }
    public IDbTransaction? Transaction => _transaction;

    public void BeginTransaction() =>
        _transaction = Connection.BeginTransaction();

    public void Commit()
    {
        _transaction?.Commit();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        Connection.Dispose();
    }
}
