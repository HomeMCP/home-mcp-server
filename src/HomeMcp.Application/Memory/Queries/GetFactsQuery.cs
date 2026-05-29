using HomeMcp.Domain.Memory;

namespace HomeMcp.Application.Memory.Queries;

public sealed record GetFactsQuery(string UserId, FactScope? Scope = null);

public sealed record FactDto(
    string Scope,
    string Key,
    string Value,
    DateTimeOffset CreatedAt);
