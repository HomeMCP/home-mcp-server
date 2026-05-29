using CSharpFunctionalExtensions;
using HomeMcp.Domain.Devices;
using HomeMcp.Domain.SharedKernel.Errors;
using HomeMcp.Domain.Users;

namespace HomeMcp.Domain.Sessions;

public interface ISessionRepository
{
    Task<Maybe<Session>> GetByIdAsync(SessionId id, CancellationToken ct = default);

    Task<Maybe<Session>> GetActiveByUserDeviceAsync(
        UserId userId,
        DeviceId deviceId,
        CancellationToken ct = default);

    Task<UnitResult<DomainError>> AddAsync(Session session, CancellationToken ct = default);

    Task<UnitResult<DomainError>> UpdateAsync(Session session, CancellationToken ct = default);

    Task<UnitResult<DomainError>> AddMessageAsync(
        SessionId sessionId,
        Message message,
        CancellationToken ct = default);

    Task<IReadOnlyList<Message>> GetMessagesAfterAsync(
        SessionId sessionId,
        long afterMessageId,
        CancellationToken ct = default);
}
