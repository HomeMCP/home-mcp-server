using HomeMcp.Domain.SharedKernel.Errors;

namespace HomeMcp.Application.Common.Errors;

public abstract record ApplicationError(string Code, string Message)
{
    public static EntityNotFoundError NotFound(string entityType, string id) =>
        new($"{entityType}.not_found", $"{entityType} '{id}' was not found.");

    public static AppValidationError Validation(string code, string message) =>
        new(code, message);

    public static InfrastructureError Infrastructure(string code, string message) =>
        new(code, message);

    public static LlmError Llm(string code, string message) =>
        new(code, message);

    public static PluginError Plugin(string pluginId, string code, string message) =>
        new(pluginId, code, message);
}

public sealed record EntityNotFoundError(string Code, string Message)
    : ApplicationError(Code, Message);

public sealed record AppValidationError(string Code, string Message)
    : ApplicationError(Code, Message);

public sealed record AppConflictError(string Code, string Message)
    : ApplicationError(Code, Message);

public sealed record InfrastructureError(string Code, string Message)
    : ApplicationError(Code, Message);

public sealed record LlmError(string Code, string Message)
    : ApplicationError(Code, Message);

public sealed record PluginError(string PluginId, string Code, string Message)
    : ApplicationError(Code, Message);

public static class DomainErrorMapper
{
    public static ApplicationError ToApplicationError(this DomainError error) => error switch
    {
        INotFoundError => new EntityNotFoundError(error.Code, error.Message),
        IConflictError => new AppConflictError(error.Code, error.Message),
        IValidationError => new AppValidationError(error.Code, error.Message),
        IUnauthorizedError => new EntityNotFoundError(error.Code, error.Message),
        IBusinessRuleError => new AppValidationError(error.Code, error.Message),
        _ => new InfrastructureError(error.Code, error.Message)
    };
}
