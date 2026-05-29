using CSharpFunctionalExtensions;
using HomeMcp.Domain.SharedKernel.Errors;

namespace HomeMcp.Domain.Users;

public interface IUserRepository
{
    Task<Maybe<User>> GetByIdAsync(UserId id, CancellationToken ct = default);
    Task<UnitResult<DomainError>> AddAsync(User user, CancellationToken ct = default);
    Task<UnitResult<DomainError>> UpdateAsync(User user, CancellationToken ct = default);
}
