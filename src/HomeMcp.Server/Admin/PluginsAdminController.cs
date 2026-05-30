using HomeMcp.Application.Plugins;
using HomeMcp.Application.Common;
using HomeMcp.Application.Common.Errors;
using HomeMcp.Domain.Plugins;
using Microsoft.AspNetCore.Mvc;
using CSharpFunctionalExtensions;
using System.Globalization;
using System.Text.Json;

namespace HomeMcp.Server.Admin;

[ApiController]
[Route("admin/plugins")]
public sealed class PluginsAdminController(
    IEnumerable<IPlugin> plugins,
    IPluginRegistry registry,
    IPluginConfigRepository configRepository,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> pluginConfigs,
    IUnitOfWork uow) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = new List<object>();

        foreach (var plugin in plugins)
        {
            var config = await GetEffectiveConfigAsync(plugin.Id, ct);
            result.Add(MapPlugin(plugin, registry, config));
        }

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var plugin = plugins.FirstOrDefault(p =>
            string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

        if (plugin is null)
        {
            return NotFound(new { error = $"Plugin '{id}' not found." });
        }

        var config = await GetEffectiveConfigAsync(plugin.Id, ct);
        return Ok(MapPlugin(plugin, registry, config));
    }

    [HttpPut("{id}/config")]
    public async Task<IActionResult> SaveConfig(
        string id,
        [FromBody] PluginConfigRequest request,
        CancellationToken ct)
    {
        var plugin = plugins.FirstOrDefault(p =>
            string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

        if (plugin is null)
        {
            return NotFound(new { error = $"Plugin '{id}' not found." });
        }

        var schema = plugin.GetConfigSchema();
        var stored = await GetEffectiveConfigAsync(plugin.Id, ct);
        var normalized = NormalizeConfig(schema, request.Config, stored);

        var validationError = ValidateConfig(schema, normalized);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        var initResult = await plugin.InitializeAsync(normalized, ct);
        if (initResult.IsFailure)
        {
            return BadRequest(new { error = initResult.Error.Message, code = initResult.Error.Code });
        }

        var saveResult = await uow.ExecuteAsync(async saveCt =>
        {
            var result = await configRepository.ReplaceConfigAsync(plugin.Id, normalized, saveCt);
            return result.IsSuccess
                ? UnitResult.Success<ApplicationError>()
                : result.Error.ToApplicationError();
        }, ct);

        if (saveResult.IsFailure)
        {
            return StatusCode(500, new { error = saveResult.Error.Message, code = saveResult.Error.Code });
        }

        registry.Register(plugin);

        var config = await GetEffectiveConfigAsync(plugin.Id, ct);
        return Ok(MapPlugin(plugin, registry, config));
    }

    private async Task<IReadOnlyDictionary<string, string>> GetEffectiveConfigAsync(
        string pluginId,
        CancellationToken ct)
    {
        var stored = await configRepository.GetConfigAsync(pluginId, ct);
        if (stored.Count > 0)
        {
            return stored;
        }

        return pluginConfigs.TryGetValue(pluginId, out var fallback)
            ? fallback
            : new Dictionary<string, string>();
    }

    private static object MapPlugin(
        IPlugin p,
        IPluginRegistry reg,
        IReadOnlyDictionary<string, string> config)
    {
        var active = reg.GetAll().Any(rp =>
            string.Equals(rp.Id, p.Id, StringComparison.OrdinalIgnoreCase));

        var schema = p.GetConfigSchema();
        var configValues = BuildConfigValues(schema, config);

        return new
        {
            id = p.Id,
            displayName = p.Manifest.DisplayName,
            version = p.Manifest.Version,
            status = active ? "active" : "inactive",
            descriptions = p.Manifest.RoutingHints.SupportedLocales
                .ToDictionary(l => l, l => p.Manifest.RoutingHints.GetDescription(l)),
            tools = p.GetTools().Select(t => new
            {
                name = t.Name,
                description = t.Description
            }),
            configSchema = schema.Select(f => new
            {
                key = f.Key,
                type = f.Type.ToString().ToLowerInvariant(),
                required = f.Required,
                labels = f.Labels,
                hints = f.Hints,
                defaultValue = f.DefaultValue.HasValue ? f.DefaultValue.Value : null,
                options = f.Options.HasValue ? f.Options.Value : null
            }),
            configValues
        };
    }

    private static Dictionary<string, object?> BuildConfigValues(
        IReadOnlyList<ConfigFieldDescriptor> schema,
        IReadOnlyDictionary<string, string> stored)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in schema)
        {
            if (field.Type == ConfigFieldType.Password)
            {
                values[field.Key] = stored.TryGetValue(field.Key, out var existing)
                    && !string.IsNullOrWhiteSpace(existing)
                    ? "__stored__"
                    : null;
                continue;
            }

            stored.TryGetValue(field.Key, out var raw);
            var value = raw ?? (field.DefaultValue.HasValue ? field.DefaultValue.Value : string.Empty);
            values[field.Key] = field.Type switch
            {
                ConfigFieldType.Boolean => bool.TryParse(value, out var b) ? b : false,
                ConfigFieldType.Integer => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
                    ? n
                    : 0,
                _ => value
            };
        }

        return values;
    }

    private static string? ValidateConfig(
        IReadOnlyList<ConfigFieldDescriptor> schema,
        IReadOnlyDictionary<string, string> config)
    {
        foreach (var field in schema)
        {
            if (!field.Required)
            {
                continue;
            }

            if (!config.TryGetValue(field.Key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                return $"Missing required config value '{field.Key}'.";
            }
        }

        return null;
    }

    private static Dictionary<string, string> NormalizeConfig(
        IReadOnlyList<ConfigFieldDescriptor> schema,
        IReadOnlyDictionary<string, JsonElement> raw,
        IReadOnlyDictionary<string, string> stored)
    {
        var allowed = schema.ToDictionary(f => f.Key, StringComparer.OrdinalIgnoreCase);
        var result = stored
            .Where(kv => allowed.ContainsKey(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in raw)
        {
            if (!allowed.TryGetValue(key, out var field))
            {
                continue;
            }

            var rawValue = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? string.Empty,
                JsonValueKind.Number => value.TryGetInt64(out var n)
                    ? n.ToString(CultureInfo.InvariantCulture)
                    : value.GetDouble().ToString(CultureInfo.InvariantCulture),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => string.Empty,
                _ => value.ToString() ?? string.Empty
            };

            if (field.Type == ConfigFieldType.Password
                && string.IsNullOrWhiteSpace(rawValue)
                && result.TryGetValue(key, out var existing))
            {
                result[key] = existing;
                continue;
            }

            result[key] = rawValue;
        }

        return result;
    }

    public sealed record PluginConfigRequest(
        IReadOnlyDictionary<string, JsonElement> Config);
}
