using CSharpFunctionalExtensions;
using HomeMcp.Application.Common;
using HomeMcp.Application.Common.Errors;
using HomeMcp.Domain.Memory;
using HomeMcp.Domain.Users;

namespace HomeMcp.Application.Memory.Commands.ForgetFact;

public sealed class ForgetFactCommandHandler : ICommandHandler<ForgetFactCommand>
{
    private readonly IFactRepository _factRepo;

    public ForgetFactCommandHandler(IFactRepository factRepo) => _factRepo = factRepo;

    public async Task<UnitResult<ApplicationError>> HandleAsync(
        ForgetFactCommand command,
        CancellationToken ct = default)
    {
        var result = await _factRepo.DeleteAsync(
            UserId.From(command.UserId),
            command.Scope,
            command.Key,
            ct);

        return result.IsSuccess
            ? UnitResult.Success<ApplicationError>()
            : result.Error.ToApplicationError();
    }
}
