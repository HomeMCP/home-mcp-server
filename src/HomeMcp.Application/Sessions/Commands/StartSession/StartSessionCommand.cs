namespace HomeMcp.Application.Sessions.Commands.StartSession;

public sealed record StartSessionCommand(
    string UserId,
    string DeviceId);

public sealed record StartSessionResult(string SessionId);
