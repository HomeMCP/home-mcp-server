namespace HomeMcp.Domain.Sessions;

public readonly record struct SessionId(string Value) : IEquatable<SessionId>
{
    public static SessionId New() => new(Guid.CreateVersion7().ToString("N"));
    public static SessionId From(string value) => new(value);
    public override string ToString() => Value;
}
