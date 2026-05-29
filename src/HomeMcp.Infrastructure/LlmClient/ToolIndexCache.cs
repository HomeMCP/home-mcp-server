using HomeMcp.Domain.Plugins;
using Microsoft.Extensions.AI;

namespace HomeMcp.Infrastructure.LlmClient;

internal sealed class ToolIndexCache
{
    public ToolIndexCache(
        string registryVersion,
        IReadOnlyDictionary<string, PluginToolEntry> lookup,
        IList<AITool> tools)
    {
        RegistryVersion = registryVersion;
        Lookup = lookup;
        Tools = tools;
    }

    public string RegistryVersion { get; }
    public IReadOnlyDictionary<string, PluginToolEntry> Lookup { get; }
    public IList<AITool> Tools { get; }
}

internal sealed record PluginToolEntry(IPlugin Plugin, ToolDefinition Tool);
