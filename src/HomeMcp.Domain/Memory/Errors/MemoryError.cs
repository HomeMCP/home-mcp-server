using HomeMcp.Domain.SharedKernel.Errors;
using HomeMcp.Domain.Users;

namespace HomeMcp.Domain.Memory.Errors;

public abstract record MemoryError(string Code, string Message) : DomainError(Code, Message);

public sealed record FactNotFoundError(UserId UserId, FactScope Scope, string Key)
    : MemoryError(MemoryErrors.Codes.FactNotFound, $"Fact '{Scope}/{Key}' not found for user '{UserId.Value}'."), INotFoundError;

public sealed record FactEmptyKeyError()
    : MemoryError(MemoryErrors.Codes.EmptyKey, "Fact key cannot be empty."), IValidationError;

public sealed record FactEmptyValueError()
    : MemoryError(MemoryErrors.Codes.EmptyValue, "Fact value cannot be empty."), IValidationError;

public static class MemoryErrors
{
    public static class Codes
    {
        public const string FactNotFound = "memory.fact_not_found";
        public const string EmptyKey = "fact.empty_key";
        public const string EmptyValue = "fact.empty_value";
    }

    public static FactNotFoundError FactNotFound(UserId userId, FactScope scope, string key) => new(userId, scope, key);
    public static FactEmptyKeyError EmptyKey() => new();
    public static FactEmptyValueError EmptyValue() => new();
}
