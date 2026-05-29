using HomeMcp.Domain.SharedKernel.Errors;

namespace HomeMcp.Domain.Users.Errors;

public abstract record UserError(string Code, string Message) : DomainError(Code, Message);

public sealed record UserNotFoundError(UserId Id)
    : UserError(UserErrors.Codes.NotFound, $"User '{Id.Value}' was not found."), INotFoundError;

public sealed record UserAlreadyExistsError(string DisplayName)
    : UserError(UserErrors.Codes.AlreadyExists, $"User '{DisplayName}' already exists."), IConflictError;

public sealed record UserEmptyNameError()
    : UserError(UserErrors.Codes.EmptyName, "Display name is required."), IValidationError;

public sealed record UserEmptyLocaleError()
    : UserError(UserErrors.Codes.EmptyLocale, "Locale is required."), IValidationError;

public static class UserErrors
{
    public static class Codes
    {
        public const string NotFound = "user.not_found";
        public const string AlreadyExists = "user.already_exists";
        public const string EmptyName = "user.empty_name";
        public const string EmptyLocale = "user.empty_locale";
    }

    public static UserNotFoundError NotFound(UserId id) => new(id);
    public static UserAlreadyExistsError AlreadyExists(string displayName) => new(displayName);
    public static UserEmptyNameError EmptyName() => new();
    public static UserEmptyLocaleError EmptyLocale() => new();
}
