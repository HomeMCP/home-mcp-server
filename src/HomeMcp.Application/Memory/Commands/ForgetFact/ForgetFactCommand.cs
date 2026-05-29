using HomeMcp.Domain.Memory;

namespace HomeMcp.Application.Memory.Commands.ForgetFact;

public sealed record ForgetFactCommand(string UserId, FactScope Scope, string Key);
