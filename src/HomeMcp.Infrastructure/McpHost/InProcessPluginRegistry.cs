using CSharpFunctionalExtensions;
using HomeMcp.Application.Plugins;
using HomeMcp.Domain.Plugins;
using HomeMcp.Domain.SharedKernel.Errors;

namespace HomeMcp.Infrastructure.McpHost;

public sealed class InProcessPluginRegistry : IPluginRegistry
{
    private readonly Dictionary<string, IPlugin> _plugins = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private string _registryVersion = string.Empty;

    public string RegistryVersion
    {
        get
        {
            lock (_lock)
            {
                return _registryVersion;
            }
        }
    }

    public IReadOnlyList<IPlugin> GetAll()
    {
        lock (_lock)
        {
            return _plugins.Values.ToList();
        }
    }

    public Maybe<IPlugin> GetById(string pluginId)
    {
        lock (_lock)
        {
            return _plugins.TryGetValue(pluginId, out var plugin)
                ? Maybe<IPlugin>.From(plugin)
                : Maybe<IPlugin>.None;
        }
    }

    public UnitResult<DomainError> Register(IPlugin plugin)
    {
        lock (_lock)
        {
            _plugins[plugin.Id] = plugin;
            _registryVersion = ComputeVersion();
        }

        return UnitResult.Success<DomainError>();
    }

    private string ComputeVersion()
    {
        var parts = _plugins.Values
            .OrderBy(p => p.Id)
            .Select(p => $"{p.Id}@{p.Manifest.Version}");
        return string.Join("|", parts);
    }
}
