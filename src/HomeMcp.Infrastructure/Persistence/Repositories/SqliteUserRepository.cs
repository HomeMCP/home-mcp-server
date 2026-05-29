using CSharpFunctionalExtensions;
using Dapper;
using HomeMcp.Domain.SharedKernel.Errors;
using HomeMcp.Domain.Users;

namespace HomeMcp.Infrastructure.Persistence.Repositories;

public sealed class SqliteUserRepository : IUserRepository
{
    private readonly DbSession _db;

    public SqliteUserRepository(DbSession db) => _db = db;

    public async Task<Maybe<User>> GetByIdAsync(UserId id, CancellationToken ct = default)
    {
        var row = await _db.Connection.QuerySingleOrDefaultAsync<UserRow>(
            "SELECT id AS Id, display_name AS DisplayName, locale AS Locale, created_at AS CreatedAt " +
            "FROM users WHERE id = @Id",
            new { Id = id.Value },
            _db.Transaction);

        return row is null
            ? Maybe<User>.None
            : User.Reconstitute(
                UserId.From(row.Id),
                row.DisplayName,
                row.Locale,
                DateTimeOffset.FromUnixTimeMilliseconds(row.CreatedAt));
    }

    public async Task<UnitResult<DomainError>> AddAsync(User user, CancellationToken ct = default)
    {
        await _db.Connection.ExecuteAsync(
            "INSERT INTO users (id, display_name, locale, created_at) VALUES (@Id, @DisplayName, @Locale, @CreatedAt)",
            new
            {
                Id = user.Id.Value,
                DisplayName = user.DisplayName,
                Locale = user.Locale,
                CreatedAt = user.CreatedAt.ToUnixTimeMilliseconds()
            },
            _db.Transaction);
        return UnitResult.Success<DomainError>();
    }

    public async Task<UnitResult<DomainError>> UpdateAsync(User user, CancellationToken ct = default)
    {
        await _db.Connection.ExecuteAsync(
            "UPDATE users SET display_name = @DisplayName, locale = @Locale WHERE id = @Id",
            new { Id = user.Id.Value, DisplayName = user.DisplayName, Locale = user.Locale },
            _db.Transaction);
        return UnitResult.Success<DomainError>();
    }

    private sealed record UserRow(string Id, string DisplayName, string Locale, long CreatedAt);
}
