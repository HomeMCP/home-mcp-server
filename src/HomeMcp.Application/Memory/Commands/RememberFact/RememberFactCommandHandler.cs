using CSharpFunctionalExtensions;
using HomeMcp.Application.Common;
using HomeMcp.Application.Common.Errors;
using HomeMcp.Domain.Memory;
using HomeMcp.Domain.Users;

namespace HomeMcp.Application.Memory.Commands.RememberFact;

public sealed class RememberFactCommandHandler : ICommandHandler<RememberFactCommand>
{
    private readonly IFactRepository _factRepo;
    private readonly TimeProvider _timeProvider;

    public RememberFactCommandHandler(IFactRepository factRepo, TimeProvider timeProvider)
    {
        _factRepo = factRepo;
        _timeProvider = timeProvider;
    }

    public async Task<UnitResult<ApplicationError>> HandleAsync(
        RememberFactCommand command,
        CancellationToken ct = default)
    {
        var factResult = Fact.Create(
            UserId.From(command.UserId),
            command.Scope,
            command.Key,
            command.Value,
            _timeProvider,
            command.Source);

        if (factResult.IsFailure)
        {
            return factResult.Error.ToApplicationError();
        }

        var upsertResult = await _factRepo.UpsertAsync(factResult.Value, ct);
        return upsertResult.IsSuccess
            ? UnitResult.Success<ApplicationError>()
            : upsertResult.Error.ToApplicationError();
    }
}
