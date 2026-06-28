using CSharpFunctionalExtensions;
using HomeMcp.Application.Common;
using HomeMcp.Application.Common.Errors;
using HomeMcp.Application.Telemetry;
using HomeMcp.Domain.Devices;
using HomeMcp.Domain.Sessions;
using HomeMcp.Domain.Users;

namespace HomeMcp.Application.Sessions.Commands.StartSession;

public sealed class StartSessionCommandHandler
    : ICommandHandler<StartSessionCommand, StartSessionResult>
{
    private readonly ISessionRepository _sessionRepo;
    private readonly IUserRepository _userRepo;
    private readonly IDeviceRepository _deviceRepo;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _timeProvider;

    public StartSessionCommandHandler(
        ISessionRepository sessionRepo,
        IUserRepository userRepo,
        IDeviceRepository deviceRepo,
        IUnitOfWork uow,
        TimeProvider timeProvider)
    {
        _sessionRepo = sessionRepo;
        _userRepo = userRepo;
        _deviceRepo = deviceRepo;
        _uow = uow;
        _timeProvider = timeProvider;
    }

    public async Task<Result<StartSessionResult, ApplicationError>> HandleAsync(
        StartSessionCommand command,
        CancellationToken ct = default)
    {
        var userId = UserId.From(command.UserId);
        var deviceId = DeviceId.From(command.DeviceId);

        var userMaybe = await _userRepo.GetByIdAsync(userId, ct);
        if (userMaybe.HasNoValue)
        {
            return ApplicationError.NotFound("user", command.UserId);
        }

        var deviceMaybe = await _deviceRepo.GetByIdAsync(deviceId, ct);
        if (deviceMaybe.HasNoValue)
        {
            return ApplicationError.NotFound("device", command.DeviceId);
        }

        var existingSession = await _sessionRepo.GetActiveByUserDeviceAsync(userId, deviceId, ct);
        if (existingSession.HasValue)
        {
            return new StartSessionResult(existingSession.Value.Id.Value);
        }

        var locale = userMaybe.Value.Locale;
        var sessionResult = Session.Create(userId, deviceId, _timeProvider, locale);
        if (sessionResult.IsFailure)
        {
            return sessionResult.Error.ToApplicationError();
        }

        var session = sessionResult.Value;

        var saveResult = await _uow.ExecuteAsync(async saveCt =>
        {
            var addResult = await _sessionRepo.AddAsync(session, saveCt);
            return addResult.IsSuccess
                ? UnitResult.Success<ApplicationError>()
                : addResult.Error.ToApplicationError();
        }, ct);

        if (saveResult.IsSuccess)
        {
            HomeMcpTelemetry.SessionsStarted.Add(1);
            return new StartSessionResult(session.Id.Value);
        }

        return Result.Failure<StartSessionResult, ApplicationError>(saveResult.Error);
    }
}
