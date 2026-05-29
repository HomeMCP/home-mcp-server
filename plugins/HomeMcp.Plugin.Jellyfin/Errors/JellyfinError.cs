using HomeMcp.Domain.SharedKernel.Errors;

namespace HomeMcp.Plugin.Jellyfin.Errors;

public abstract record JellyfinError(string Code, string Message) : DomainError(Code, Message);

public sealed record JellyfinNotInitializedError()
    : JellyfinError(JellyfinErrors.Codes.NotInitialized, "Jellyfin plugin is not initialized."), IBusinessRuleError;

public sealed record JellyfinUnknownToolError(string ToolName)
    : JellyfinError(JellyfinErrors.Codes.UnknownTool, $"Unknown tool '{ToolName}'."), INotFoundError;

public sealed record JellyfinRuntimeError(string Detail)
    : JellyfinError(JellyfinErrors.Codes.RuntimeError, Detail), IBusinessRuleError;

public sealed record JellyfinMissingBaseUrlError()
    : JellyfinError(JellyfinErrors.Codes.MissingBaseUrl, "baseUrl is required."), IValidationError;

public sealed record JellyfinMissingApiKeyError()
    : JellyfinError(JellyfinErrors.Codes.MissingApiKey, "apiKey is required."), IValidationError;

public sealed record JellyfinEmptyQueryError()
    : JellyfinError(JellyfinErrors.Codes.EmptyQuery, "query cannot be empty."), IValidationError;

public sealed record JellyfinEmptyItemIdError()
    : JellyfinError(JellyfinErrors.Codes.EmptyItemId, "item_id cannot be empty."), IValidationError;

public sealed record JellyfinTrackNotFoundError(string ItemId)
    : JellyfinError(JellyfinErrors.Codes.TrackNotFound, $"Track '{ItemId}' not found."), INotFoundError;

public static class JellyfinErrors
{
    public static class Codes
    {
        public const string NotInitialized = "jellyfin.not_initialized";
        public const string UnknownTool = "jellyfin.unknown_tool";
        public const string RuntimeError = "jellyfin.error";
        public const string MissingBaseUrl = "jellyfin.missing_base_url";
        public const string MissingApiKey = "jellyfin.missing_api_key";
        public const string EmptyQuery = "jellyfin.empty_query";
        public const string EmptyItemId = "jellyfin.empty_item_id";
        public const string TrackNotFound = "jellyfin.track_not_found";
    }

    public static JellyfinNotInitializedError NotInitialized() => new();
    public static JellyfinUnknownToolError UnknownTool(string toolName) => new(toolName);
    public static JellyfinRuntimeError RuntimeError(string detail) => new(detail);
    public static JellyfinMissingBaseUrlError MissingBaseUrl() => new();
    public static JellyfinMissingApiKeyError MissingApiKey() => new();
    public static JellyfinEmptyQueryError EmptyQuery() => new();
    public static JellyfinEmptyItemIdError EmptyItemId() => new();
    public static JellyfinTrackNotFoundError TrackNotFound(string itemId) => new(itemId);
}
