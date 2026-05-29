using CSharpFunctionalExtensions;
using HomeMcp.Domain.SharedKernel.Errors;
using HomeMcp.Domain.Users;

namespace HomeMcp.Domain.Memory;

public interface IFactRepository
{
    Task<IReadOnlyList<Fact>> GetByUserAsync(
        UserId userId,
        FactScope? scope = null,
        CancellationToken ct = default);

    Task<UnitResult<DomainError>> UpsertAsync(Fact fact, CancellationToken ct = default);

    Task<UnitResult<DomainError>> DeleteAsync(
        UserId userId,
        FactScope scope,
        string key,
        CancellationToken ct = default);
}
