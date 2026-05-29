using CSharpFunctionalExtensions;
using HomeMcp.Application.Common.Errors;

namespace HomeMcp.Application.Common;

public interface ICommandHandler<TCommand, TResult>
{
    Task<Result<TResult, ApplicationError>> HandleAsync(
        TCommand command,
        CancellationToken ct = default);
}

public interface ICommandHandler<TCommand>
{
    Task<UnitResult<ApplicationError>> HandleAsync(
        TCommand command,
        CancellationToken ct = default);
}
