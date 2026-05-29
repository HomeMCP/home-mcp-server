using CSharpFunctionalExtensions;
using HomeMcp.Application.Common.Errors;

namespace HomeMcp.Application.Common;

public interface IUnitOfWork
{
    Task<UnitResult<ApplicationError>> ExecuteAsync(
        Func<CancellationToken, Task<UnitResult<ApplicationError>>> work,
        CancellationToken ct = default);
}
