using CSharpFunctionalExtensions;
using HomeMcp.Domain.SharedKernel;
using HomeMcp.Domain.Users.Errors;

namespace HomeMcp.Domain.Users;

public class User : AggregateRoot<UserId>
{
    private User(UserId id, string displayName, string locale, DateTimeOffset createdAt) : base(id)
    {
        DisplayName = displayName;
        Locale = locale;
        CreatedAt = createdAt;
    }

    public string DisplayName { get; private set; }
    public string Locale { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public static Result<User, UserError> Create(
        string displayName,
        TimeProvider timeProvider,
        string locale = "ru-RU")
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return UserErrors.EmptyName();
        }

        if (string.IsNullOrWhiteSpace(locale))
        {
            return UserErrors.EmptyLocale();
        }

        return new User(UserId.New(), displayName.Trim(), locale, timeProvider.GetUtcNow());
    }

    public static User Reconstitute(UserId id, string displayName, string locale, DateTimeOffset createdAt) =>
        new(id, displayName, locale, createdAt);

    public void UpdateDisplayName(string displayName) => DisplayName = displayName;

    public void UpdateLocale(string locale) => Locale = locale;
}
