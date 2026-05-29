using CSharpFunctionalExtensions;
using HomeMcp.Application.Common.Errors;

namespace HomeMcp.Application.Common;

public interface IQueryHandler<TQuery, TResult>
{
    Task<Result<TResult, ApplicationError>> HandleAsync(
        TQuery query,
        CancellationToken ct = default);
}
