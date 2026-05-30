using HomeMcp.Application.Common;
using HomeMcp.Application.Orchestration;
using HomeMcp.Application.Plugins;
using HomeMcp.Domain.Devices;
using HomeMcp.Domain.Memory;
using HomeMcp.Domain.Plugins;
using HomeMcp.Domain.Sessions;
using HomeMcp.Domain.Users;
using HomeMcp.Infrastructure.LlmClient;
using HomeMcp.Infrastructure.McpHost;
using HomeMcp.Infrastructure.Persistence;
using HomeMcp.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OllamaSharp;

namespace HomeMcp.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        InfrastructureOptions options)
    {
        // TimeProvider - Singleton, injected everywhere time is needed
        services.AddSingleton(TimeProvider.System);

        // DB connection factory - Singleton (creates new connections on demand)
        services.AddSingleton<IDbConnectionFactory>(
            new SqliteConnectionFactory(options.SqliteConnectionString));

        // DbSession - Scoped: one shared connection+transaction per request
        services.AddScoped<DbSession>();

        // Repositories - Scoped (share DbSession within same request)
        services.AddScoped<ISessionRepository, SqliteSessionRepository>();
        services.AddScoped<IUserRepository, SqliteUserRepository>();
        services.AddScoped<IDeviceRepository, SqliteDeviceRepository>();
        services.AddScoped<IFactRepository, SqliteFactRepository>();
        services.AddScoped<IPluginConfigRepository, SqlitePluginConfigRepository>();

        // Unit of Work - Scoped (wraps the scoped DbSession for transactions)
        services.AddScoped<IUnitOfWork, SqliteUnitOfWork>();

        // Plugin registry - Singleton (plugins registered at startup, read on every request)
        services.AddSingleton<IPluginRegistry, InProcessPluginRegistry>();

        // LLM client - Singleton (thread-safe; owns HttpMessageHandler pool)
        services.AddSingleton<IChatClient>(_ =>
        {
            var client = new OllamaApiClient(new Uri(options.OllamaUrl));
            client.SelectedModel = options.ChatModel;
            return client;
        });

        // Chat orchestrator - Singleton (holds tool index cache; IChatClient is also Singleton)
        services.AddSingleton<IChatOrchestrator, OllamaChatOrchestrator>();

        // Database migrator
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatabaseMigrator>>();
            return new DatabaseMigrator(options.SqliteConnectionString, logger);
        });

        // Named HttpClients for plugins (IHttpClientFactory manages socket lifecycle)
        services.AddHttpClient("jellyfin");
        services.AddHttpClient("homeassistant");

        return services;
    }
}

public sealed record InfrastructureOptions(
    string SqliteConnectionString,
    string OllamaUrl,
    string ChatModel);
