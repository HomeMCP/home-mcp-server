using CSharpFunctionalExtensions;
using HomeMcp.Domain.Memory.Errors;
using HomeMcp.Domain.Users;

namespace HomeMcp.Domain.Memory;

public class Fact : SharedKernel.Entity<FactId>
{
    private Fact(
        FactId id,
        UserId userId,
        FactScope scope,
        string key,
        string value,
        Maybe<string> source,
        DateTimeOffset createdAt) : base(id)
    {
        UserId = userId;
        Scope = scope;
        Key = key;
        Value = value;
        Source = source;
        CreatedAt = createdAt;
    }

    public UserId UserId { get; }
    public FactScope Scope { get; }
    public string Key { get; }
    public string Value { get; private set; }
    public Maybe<string> Source { get; }
    public DateTimeOffset CreatedAt { get; }

    public static Result<Fact, MemoryError> Create(
        UserId userId,
        FactScope scope,
        string key,
        string value,
        TimeProvider timeProvider,
        Maybe<string> source = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return MemoryErrors.EmptyKey();
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return MemoryErrors.EmptyValue();
        }

        return new Fact(new FactId(0), userId, scope, key.Trim(), value, source, timeProvider.GetUtcNow());
    }

    public static Fact Reconstitute(
        FactId id,
        UserId userId,
        FactScope scope,
        string key,
        string value,
        Maybe<string> source,
        DateTimeOffset createdAt) =>
        new(id, userId, scope, key, value, source, createdAt);

    public void UpdateValue(string value) => Value = value;
}
