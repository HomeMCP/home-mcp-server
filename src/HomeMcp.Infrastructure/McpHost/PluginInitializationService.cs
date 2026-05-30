using HomeMcp.Application.Plugins;
using HomeMcp.Domain.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeMcp.Infrastructure.McpHost;

public sealed class PluginInitializationService : IHostedService
{
    private readonly IEnumerable<IPlugin> _plugins;
    private readonly IPluginRegistry _registry;
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _pluginConfigs;
    private readonly ILogger<PluginInitializationService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public PluginInitializationService(
        IEnumerable<IPlugin> plugins,
        IPluginRegistry registry,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> pluginConfigs,
        IServiceScopeFactory scopeFactory,
        ILogger<PluginInitializationService> logger)
    {
        _plugins = plugins;
        _registry = registry;
        _pluginConfigs = pluginConfigs;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var configRepository = scope.ServiceProvider.GetRequiredService<IPluginConfigRepository>();

        foreach (var plugin in _plugins)
        {
            _logger.LogInformation("Initializing plugin '{PluginId}' v{Version}.",
                plugin.Id, plugin.Manifest.Version);

            var storedConfig = await configRepository.GetConfigAsync(plugin.Id, cancellationToken);

            var config = storedConfig.Count > 0
                ? storedConfig
                : _pluginConfigs.TryGetValue(plugin.Id, out var cfg)
                    ? cfg
                    : new Dictionary<string, string>();

            var result = await plugin.InitializeAsync(config, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError(
                    "Plugin '{PluginId}' failed to initialize: [{Code}] {Message}",
                    plugin.Id, result.Error.Code, result.Error.Message);
                continue;
            }

            _registry.Register(plugin);
            _logger.LogInformation("Plugin '{PluginId}' registered successfully.", plugin.Id);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
