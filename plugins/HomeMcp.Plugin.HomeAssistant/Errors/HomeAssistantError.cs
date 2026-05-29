using HomeMcp.Domain.SharedKernel.Errors;

namespace HomeMcp.Plugin.HomeAssistant.Errors;

public abstract record HomeAssistantError(string Code, string Message) : DomainError(Code, Message);

public sealed record HomeAssistantNotInitializedError()
    : HomeAssistantError(HomeAssistantErrors.Codes.NotInitialized, "HomeAssistant plugin is not initialized."), IBusinessRuleError;

public sealed record HomeAssistantUnknownToolError(string ToolName)
    : HomeAssistantError(HomeAssistantErrors.Codes.UnknownTool, $"Unknown tool '{ToolName}'."), INotFoundError;

public sealed record HomeAssistantRuntimeError(string Detail)
    : HomeAssistantError(HomeAssistantErrors.Codes.RuntimeError, Detail), IBusinessRuleError;

public sealed record HomeAssistantMissingBaseUrlError()
    : HomeAssistantError(HomeAssistantErrors.Codes.MissingBaseUrl, "baseUrl is required."), IValidationError;

public sealed record HomeAssistantMissingTokenError()
    : HomeAssistantError(HomeAssistantErrors.Codes.MissingToken, "token is required."), IValidationError;

public sealed record HomeAssistantMissingEntityIdError()
    : HomeAssistantError(HomeAssistantErrors.Codes.MissingEntityId, "entity_id is required."), IValidationError;

public static class HomeAssistantErrors
{
    public static class Codes
    {
        public const string NotInitialized = "ha.not_initialized";
        public const string UnknownTool = "ha.unknown_tool";
        public const string RuntimeError = "ha.error";
        public const string MissingBaseUrl = "ha.missing_base_url";
        public const string MissingToken = "ha.missing_token";
        public const string MissingEntityId = "ha.missing_entity_id";
    }

    public static HomeAssistantNotInitializedError NotInitialized() => new();
    public static HomeAssistantUnknownToolError UnknownTool(string toolName) => new(toolName);
    public static HomeAssistantRuntimeError RuntimeError(string detail) => new(detail);
    public static HomeAssistantMissingBaseUrlError MissingBaseUrl() => new();
    public static HomeAssistantMissingTokenError MissingToken() => new();
    public static HomeAssistantMissingEntityIdError MissingEntityId() => new();
}
