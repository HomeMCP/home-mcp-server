namespace HomeMcp.Domain.Memory;

public readonly record struct FactId(long Value) : IEquatable<FactId>
{
    public static FactId From(long value) => new(value);
    public override string ToString() => Value.ToString();
}
