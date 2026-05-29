using CSharpFunctionalExtensions;
using HomeMcp.Application.Common;
using HomeMcp.Application.Common.Errors;
using Microsoft.Extensions.Logging;

namespace HomeMcp.Infrastructure.Persistence;

public sealed class SqliteUnitOfWork : IUnitOfWork
{
    private readonly DbSession _session;
    private readonly ILogger<SqliteUnitOfWork> _logger;

    public SqliteUnitOfWork(DbSession session, ILogger<SqliteUnitOfWork> logger)
    {
        _session = session;
        _logger = logger;
    }

    public async Task<UnitResult<ApplicationError>> ExecuteAsync(
        Func<CancellationToken, Task<UnitResult<ApplicationError>>> work,
        CancellationToken ct = default)
    {
        _session.BeginTransaction();
        try
        {
            var result = await work(ct);

            if (result.IsSuccess)
            {
                _session.Commit();
            }
            else
            {
                _session.Rollback();
                _logger.LogWarning("Transaction rolled back: [{Code}] {Message}", result.Error.Code, result.Error.Message);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            _session.Rollback();
            throw;
        }
        catch (Exception ex)
        {
            _session.Rollback();
            _logger.LogError(ex, "Unhandled exception during transaction.");
            return ApplicationError.Infrastructure("db.transaction_failed", ex.Message);
        }
    }
}
