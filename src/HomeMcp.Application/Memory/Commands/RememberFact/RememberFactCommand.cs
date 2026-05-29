using CSharpFunctionalExtensions;
using HomeMcp.Domain.Memory;

namespace HomeMcp.Application.Memory.Commands.RememberFact;

public sealed record RememberFactCommand(
    string UserId,
    FactScope Scope,
    string Key,
    string Value,
    Maybe<string> Source = default);
