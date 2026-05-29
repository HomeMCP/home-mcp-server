namespace HomeMcp.Application.Sessions.Queries.GetSession;

public sealed record GetSessionQuery(string SessionId);

public sealed record GetSessionResult(
    string SessionId,
    string UserId,
    string DeviceId,
    DateTimeOffset StartedAt,
    DateTimeOffset LastActiveAt,
    int MessageCount);
