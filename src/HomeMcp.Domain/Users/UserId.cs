namespace HomeMcp.Domain.Users;

public readonly record struct UserId(string Value) : IEquatable<UserId>
{
    public static UserId New() => new(Guid.CreateVersion7().ToString("N"));
    public static UserId From(string value) => new(value);
    public override string ToString() => Value;
}
