using CSharpFunctionalExtensions;
using Dapper;
using HomeMcp.Domain.Devices;
using HomeMcp.Domain.Sessions;
using HomeMcp.Domain.SharedKernel.Errors;
using HomeMcp.Domain.Users;

namespace HomeMcp.Infrastructure.Persistence.Repositories;

public sealed class SqliteSessionRepository : ISessionRepository
{
    private readonly DbSession _db;
    private readonly TimeProvider _timeProvider;

    public SqliteSessionRepository(DbSession db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Maybe<Session>> GetByIdAsync(SessionId id, CancellationToken ct = default)
    {
        var row = await _db.Connection.QuerySingleOrDefaultAsync<SessionRow>(
            @"SELECT id AS Id,
                       user_id AS UserId,
                       device_id AS DeviceId,
                       locale AS Locale,
                       started_at AS StartedAt,
                       last_active_at AS LastActiveAt,
                       summary AS Summary,
                       summary_until_message_id AS SummaryUntilMessageId
                FROM sessions
                WHERE id = @Id",
            new { Id = id.Value },
            _db.Transaction);

        if (row is null)
        {
            return Maybe<Session>.None;
        }

        var messages = await LoadMessages(id.Value);
        return Reconstitute(row, messages);
    }

    public async Task<Maybe<Session>> GetActiveByUserDeviceAsync(
        UserId userId,
        DeviceId deviceId,
        CancellationToken ct = default)
    {
        var row = await _db.Connection.QuerySingleOrDefaultAsync<SessionRow>(
            @"SELECT id AS Id,
                     user_id AS UserId,
                     device_id AS DeviceId,
                     locale AS Locale,
                     started_at AS StartedAt,
                     last_active_at AS LastActiveAt,
                     summary AS Summary,
                     summary_until_message_id AS SummaryUntilMessageId
              FROM sessions
              WHERE user_id = @UserId AND device_id = @DeviceId
              ORDER BY last_active_at DESC
              LIMIT 1",
            new { UserId = userId.Value, DeviceId = deviceId.Value },
            _db.Transaction);

        if (row is null)
        {
            return Maybe<Session>.None;
        }

        var messages = await LoadMessages(row.Id);
        return Reconstitute(row, messages);
    }

    public async Task<UnitResult<DomainError>> AddAsync(Session session, CancellationToken ct = default)
    {
        await _db.Connection.ExecuteAsync(
            @"INSERT INTO sessions (id, user_id, device_id, locale, started_at, last_active_at)
              VALUES (@Id, @UserId, @DeviceId, @Locale, @StartedAt, @LastActiveAt)",
            new
            {
                Id = session.Id.Value,
                UserId = session.UserId.Value,
                DeviceId = session.DeviceId.Value,
                Locale = session.Locale,
                StartedAt = session.StartedAt.ToUnixTimeMilliseconds(),
                LastActiveAt = session.LastActiveAt.ToUnixTimeMilliseconds()
            },
            _db.Transaction);
        return UnitResult.Success<DomainError>();
    }

    public async Task<UnitResult<DomainError>> UpdateAsync(Session session, CancellationToken ct = default)
    {
        await _db.Connection.ExecuteAsync(
            @"UPDATE sessions SET
                last_active_at = @LastActiveAt,
                summary = @Summary,
                summary_until_message_id = @SummaryUntilMessageId
              WHERE id = @Id",
            new
            {
                Id = session.Id.Value,
                LastActiveAt = session.LastActiveAt.ToUnixTimeMilliseconds(),
                Summary = session.Summary.GetValueOrDefault(),
                SummaryUntilMessageId = session.SummaryUntilMessageId.HasValue
                    ? session.SummaryUntilMessageId.Value
                    : (long?)null
            },
            _db.Transaction);
        return UnitResult.Success<DomainError>();
    }

    public async Task<UnitResult<DomainError>> AddMessageAsync(
        SessionId sessionId,
        Message message,
        CancellationToken ct = default)
    {
        await _db.Connection.ExecuteAsync(
            @"INSERT INTO messages (session_id, role, content, tool_calls, created_at)
              VALUES (@SessionId, @Role, @Content, @ToolCalls, @CreatedAt)",
            new
            {
                SessionId = sessionId.Value,
                Role = message.Role.ToString().ToLowerInvariant(),
                Content = message.Content,
                ToolCalls = message.ToolCallsJson.GetValueOrDefault(),
                CreatedAt = message.CreatedAt.ToUnixTimeMilliseconds()
            },
            _db.Transaction);
        return UnitResult.Success<DomainError>();
    }

    public async Task<IReadOnlyList<Message>> GetMessagesAfterAsync(
        SessionId sessionId,
        long afterMessageId,
        CancellationToken ct = default)
    {
        var rows = await _db.Connection.QueryAsync<MessageRow>(
            @"SELECT id AS Id,
                       session_id AS SessionId,
                       role AS Role,
                       content AS Content,
                       tool_calls AS ToolCalls,
                       created_at AS CreatedAt
                FROM messages
                WHERE session_id = @SessionId AND id > @AfterId
                ORDER BY id",
            new { SessionId = sessionId.Value, AfterId = afterMessageId },
            _db.Transaction);

        return rows.Select(MapMessage).ToList();
    }

    private async Task<IReadOnlyList<Message>> LoadMessages(string sessionId)
    {
        var rows = await _db.Connection.QueryAsync<MessageRow>(
            @"SELECT id AS Id,
                       session_id AS SessionId,
                       role AS Role,
                       content AS Content,
                       tool_calls AS ToolCalls,
                       created_at AS CreatedAt
                FROM messages
                WHERE session_id = @SessionId
                ORDER BY id",
            new { SessionId = sessionId },
            _db.Transaction);

        return rows.Select(MapMessage).ToList();
    }

    private static Message MapMessage(MessageRow r) => Message.Reconstitute(
        MessageId.From(r.Id),
        SessionId.From(r.SessionId),
        Enum.TryParse<MessageRole>(r.Role, ignoreCase: true, out var role) ? role : MessageRole.User,
        r.Content,
        Maybe.From(r.ToolCalls),
        DateTimeOffset.FromUnixTimeMilliseconds(r.CreatedAt));

    private Session Reconstitute(SessionRow row, IReadOnlyList<Message> messages) =>
        Session.Reconstitute(
            SessionId.From(row.Id),
            UserId.From(row.UserId),
            DeviceId.From(row.DeviceId),
            row.Locale ?? "ru-RU",
            DateTimeOffset.FromUnixTimeMilliseconds(row.StartedAt),
            DateTimeOffset.FromUnixTimeMilliseconds(row.LastActiveAt),
            Maybe.From(row.Summary),
            row.SummaryUntilMessageId is { } uid ? Maybe.From(uid) : Maybe<long>.None,
            messages,
            _timeProvider);

    private sealed record SessionRow(
        string Id,
        string UserId,
        string DeviceId,
        string? Locale,
        long StartedAt,
        long LastActiveAt,
        string? Summary,
        long? SummaryUntilMessageId);

    private sealed record MessageRow(
        long Id,
        string SessionId,
        string Role,
        string Content,
        string? ToolCalls,
        long CreatedAt);
}
