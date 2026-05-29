namespace HomeMcp.Domain.Sessions;

public readonly record struct MessageId(long Value) : IEquatable<MessageId>
{
    public static MessageId From(long value) => new(value);
    public override string ToString() => Value.ToString();
}
