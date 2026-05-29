using HomeMcp.Domain.SharedKernel.Errors;

namespace HomeMcp.Domain.Devices.Errors;

public abstract record DeviceError(string Code, string Message) : DomainError(Code, Message);

public sealed record DeviceNotFoundError(DeviceId Id)
    : DeviceError(DeviceErrors.Codes.NotFound, $"Device '{Id.Value}' was not found."), INotFoundError;

public sealed record DeviceInvalidTokenError(DeviceId Id)
    : DeviceError(DeviceErrors.Codes.InvalidToken, $"Invalid token for device '{Id.Value}'."), IUnauthorizedError;

public sealed record DeviceEmptyTokenError()
    : DeviceError(DeviceErrors.Codes.EmptyToken, "Token hash is required."), IValidationError;

public static class DeviceErrors
{
    public static class Codes
    {
        public const string NotFound = "device.not_found";
        public const string InvalidToken = "device.invalid_token";
        public const string EmptyToken = "device.empty_token";
    }

    public static DeviceNotFoundError NotFound(DeviceId id) => new(id);
    public static DeviceInvalidTokenError InvalidToken(DeviceId id) => new(id);
    public static DeviceEmptyTokenError EmptyToken() => new();
}
