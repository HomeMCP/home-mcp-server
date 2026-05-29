namespace HomeMcp.Domain.Plugins;

public sealed record ToolDefinition(
    string PluginId,
    string Name,
    string Description,
    string ParametersSchemaJson);
