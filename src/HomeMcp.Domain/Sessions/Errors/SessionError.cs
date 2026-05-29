using HomeMcp.Domain.SharedKernel.Errors;

namespace HomeMcp.Domain.Sessions.Errors;

public abstract record SessionError(string Code, string Message) : DomainError(Code, Message);

public sealed record SessionNotFoundError(SessionId Id)
    : SessionError(SessionErrors.Codes.NotFound, $"Session '{Id.Value}' was not found."), INotFoundError;

public sealed record SessionExpiredError(SessionId Id)
    : SessionError(SessionErrors.Codes.Expired, $"Session '{Id.Value}' has expired."), IBusinessRuleError;

public sealed record SessionEmptyInputError()
    : SessionError(SessionErrors.Codes.EmptyInput, "User input cannot be empty."), IValidationError;

public sealed record SessionInvalidUserError()
    : SessionError(SessionErrors.Codes.InvalidUser, "UserId is required."), IValidationError;

public sealed record SessionInvalidDeviceError()
    : SessionError(SessionErrors.Codes.InvalidDevice, "DeviceId is required."), IValidationError;

public sealed record SessionInvalidLocaleError()
    : SessionError(SessionErrors.Codes.InvalidLocale, "Locale is required."), IValidationError;

public static class SessionErrors
{
    public static class Codes
    {
        public const string NotFound = "session.not_found";
        public const string Expired = "session.expired";
        public const string EmptyInput = "session.empty_input";
        public const string InvalidUser = "session.invalid_user";
        public const string InvalidDevice = "session.invalid_device";
        public const string InvalidLocale = "session.invalid_locale";
    }

    public static SessionNotFoundError NotFound(SessionId id) => new(id);
    public static SessionExpiredError Expired(SessionId id) => new(id);
    public static SessionEmptyInputError EmptyInput() => new();
    public static SessionInvalidUserError InvalidUser() => new();
    public static SessionInvalidDeviceError InvalidDevice() => new();
    public static SessionInvalidLocaleError InvalidLocale() => new();
}
