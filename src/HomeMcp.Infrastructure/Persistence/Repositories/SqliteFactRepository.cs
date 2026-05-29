using CSharpFunctionalExtensions;
using Dapper;
using HomeMcp.Domain.Memory;
using HomeMcp.Domain.SharedKernel.Errors;
using HomeMcp.Domain.Users;

namespace HomeMcp.Infrastructure.Persistence.Repositories;

public sealed class SqliteFactRepository : IFactRepository
{
    private readonly DbSession _db;

    public SqliteFactRepository(DbSession db) => _db = db;

    public async Task<IReadOnlyList<Fact>> GetByUserAsync(
        UserId userId,
        FactScope? scope = null,
        CancellationToken ct = default)
    {
        const string columns = @"SELECT id AS Id,
       user_id AS UserId,
       scope AS Scope,
       key AS Key,
       value AS Value,
       source AS Source,
       created_at AS CreatedAt
FROM facts";

        var sql = scope.HasValue
            ? $"{columns} WHERE user_id = @UserId AND scope = @Scope ORDER BY id DESC LIMIT 100"
            : $"{columns} WHERE user_id = @UserId ORDER BY id DESC LIMIT 100";

        var rows = await _db.Connection.QueryAsync<FactRow>(
            sql,
            new { UserId = userId.Value, Scope = scope?.ToString() },
            _db.Transaction);

        return rows.Select(Reconstitute).ToList();
    }

    public async Task<UnitResult<DomainError>> UpsertAsync(Fact fact, CancellationToken ct = default)
    {
        await _db.Connection.ExecuteAsync(
            @"INSERT INTO facts (user_id, scope, key, value, source, created_at)
              VALUES (@UserId, @Scope, @Key, @Value, @Source, @CreatedAt)
              ON CONFLICT(user_id, scope, key) DO UPDATE SET value = excluded.value",
            new
            {
                UserId = fact.UserId.Value,
                Scope = fact.Scope.ToString(),
                Key = fact.Key,
                Value = fact.Value,
                Source = fact.Source.GetValueOrDefault(),
                CreatedAt = fact.CreatedAt.ToUnixTimeMilliseconds()
            },
            _db.Transaction);
        return UnitResult.Success<DomainError>();
    }

    public async Task<UnitResult<DomainError>> DeleteAsync(
        UserId userId,
        FactScope scope,
        string key,
        CancellationToken ct = default)
    {
        await _db.Connection.ExecuteAsync(
            "DELETE FROM facts WHERE user_id = @UserId AND scope = @Scope AND key = @Key",
            new { UserId = userId.Value, Scope = scope.ToString(), Key = key },
            _db.Transaction);
        return UnitResult.Success<DomainError>();
    }

    private static Fact Reconstitute(FactRow row) => Fact.Reconstitute(
        FactId.From(row.Id),
        UserId.From(row.UserId),
        Enum.TryParse<FactScope>(row.Scope, out var scope) ? scope : FactScope.Misc,
        row.Key,
        row.Value,
        Maybe.From(row.Source),
        DateTimeOffset.FromUnixTimeMilliseconds(row.CreatedAt));

    private sealed record FactRow(
        long Id,
        string UserId,
        string Scope,
        string Key,
        string Value,
        string? Source,
        long CreatedAt);
}
