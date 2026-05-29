using CSharpFunctionalExtensions;
using HomeMcp.Application.Common;
using HomeMcp.Application.Common.Errors;
using HomeMcp.Domain.Sessions;

namespace HomeMcp.Application.Sessions.Queries.GetSession;

public sealed class GetSessionQueryHandler : IQueryHandler<GetSessionQuery, GetSessionResult>
{
    private readonly ISessionRepository _sessionRepo;

    public GetSessionQueryHandler(ISessionRepository sessionRepo) =>
        _sessionRepo = sessionRepo;

    public async Task<Result<GetSessionResult, ApplicationError>> HandleAsync(
        GetSessionQuery query,
        CancellationToken ct = default)
    {
        var sessionMaybe = await _sessionRepo.GetByIdAsync(SessionId.From(query.SessionId), ct);

        return sessionMaybe.HasNoValue
            ? ApplicationError.NotFound("session", query.SessionId)
            : new GetSessionResult(
                sessionMaybe.Value.Id.Value,
                sessionMaybe.Value.UserId.Value,
                sessionMaybe.Value.DeviceId.Value,
                sessionMaybe.Value.StartedAt,
                sessionMaybe.Value.LastActiveAt,
                sessionMaybe.Value.Messages.Count);
    }
}
