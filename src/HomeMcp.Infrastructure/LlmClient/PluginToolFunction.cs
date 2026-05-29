using System.Text.Json;
using HomeMcp.Domain.Plugins;
using Microsoft.Extensions.AI;

namespace HomeMcp.Infrastructure.LlmClient;

internal sealed class PluginToolFunction : AIFunction
{
    private readonly IPlugin _plugin;
    private readonly string _toolName;
    private readonly string _description;
    private readonly JsonElement _jsonSchema;

    public PluginToolFunction(IPlugin plugin, ToolDefinition tool)
    {
        _plugin = plugin;
        _toolName = tool.Name;
        _description = tool.Description;
        _jsonSchema = JsonDocument.Parse(tool.ParametersSchemaJson).RootElement.Clone();
    }

    public override string Name => _toolName;
    public override string Description => _description;
    public override JsonElement JsonSchema => _jsonSchema;

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        var argsJson = JsonSerializer.Serialize(
            arguments.ToDictionary(kv => kv.Key, kv => kv.Value));
        var result = await _plugin.ExecuteToolAsync(_toolName, argsJson, cancellationToken);
        return result.IsSuccess ? result.Value : $"Error: {result.Error?.Message}";
    }
}
