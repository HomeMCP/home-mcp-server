namespace HomeMcp.Domain.SharedKernel;

public abstract class Entity<TId> where TId : IEquatable<TId>
{
    protected Entity(TId id) => Id = id;

    public TId Id { get; }

    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && GetType() == other.GetType() && Id.Equals(other.Id);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? a, Entity<TId>? b) =>
        a is null && b is null || a is not null && b is not null && a.Equals(b);

    public static bool operator !=(Entity<TId>? a, Entity<TId>? b) => !(a == b);
}
