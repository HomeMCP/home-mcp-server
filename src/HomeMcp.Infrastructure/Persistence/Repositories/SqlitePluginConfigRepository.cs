using CSharpFunctionalExtensions;
using Dapper;
using HomeMcp.Domain.Plugins;
using HomeMcp.Domain.SharedKernel.Errors;

namespace HomeMcp.Infrastructure.Persistence.Repositories;

public sealed class SqlitePluginConfigRepository : IPluginConfigRepository
{
    private readonly DbSession _db;

    public SqlitePluginConfigRepository(DbSession db) => _db = db;

    public async Task<IReadOnlyDictionary<string, string>> GetConfigAsync(
        string pluginId,
        CancellationToken ct = default)
    {
        var rows = await _db.Connection.QueryAsync<ConfigRow>(
            "SELECT key AS Key, value AS Value FROM plugin_runtime_config WHERE plugin_id = @PluginId",
            new { PluginId = pluginId },
            _db.Transaction);

        return rows.ToDictionary(r => r.Key, r => r.Value, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<UnitResult<DomainError>> ReplaceConfigAsync(
        string pluginId,
        IReadOnlyDictionary<string, string> config,
        CancellationToken ct = default)
    {
        await _db.Connection.ExecuteAsync(
            "DELETE FROM plugin_runtime_config WHERE plugin_id = @PluginId",
            new { PluginId = pluginId },
            _db.Transaction);

        const string insertSql = @"INSERT INTO plugin_runtime_config
(plugin_id, key, value, updated_at)
VALUES (@PluginId, @Key, @Value, @UpdatedAt)";

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var (key, value) in config)
        {
            await _db.Connection.ExecuteAsync(
                insertSql,
                new
                {
                    PluginId = pluginId,
                    Key = key,
                    Value = value,
                    UpdatedAt = now
                },
                _db.Transaction);
        }

        return UnitResult.Success<DomainError>();
    }

    private sealed record ConfigRow(string Key, string Value);
}
