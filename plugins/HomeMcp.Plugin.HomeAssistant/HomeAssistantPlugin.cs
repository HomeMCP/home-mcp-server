using System.Net.Http.Headers;
using System.Text.Json;
using CSharpFunctionalExtensions;
using HomeMcp.Domain.Plugins;
using HomeMcp.Domain.SharedKernel;
using HomeMcp.Domain.SharedKernel.Errors;
using HomeMcp.Plugin.HomeAssistant.Errors;

namespace HomeMcp.Plugin.HomeAssistant;

public sealed class HomeAssistantPlugin : IPlugin
{
    private readonly IHttpClientFactory _httpClientFactory;
    private Maybe<HomeAssistantClient> _client = Maybe<HomeAssistantClient>.None;
    private WorldContext _worldContext = new([], null);

    public HomeAssistantPlugin(IHttpClientFactory httpClientFactory) =>
        _httpClientFactory = httpClientFactory;

    public string Id => "io.homeassistant";

    public PluginManifest Manifest { get; } = new(
        id: "io.homeassistant",
        version: "0.1.0",
        displayName: "Home Assistant",
        descriptions: new Dictionary<string, string>
        {
            ["en"] = "Control smart home devices via Home Assistant API.",
            ["ru"] = "Управление умным домом через Home Assistant.",
        },
        routingHints: new PluginRoutingHints(
            descriptions: new Dictionary<string, string>
            {
                ["en"] = "Smart home control: lights, switches, climate, sensors.",
                ["ru"] = "Управление умным домом: свет, розетки, климат, датчики.",
            },
            keywords: new Dictionary<string, IReadOnlyList<string>>
            {
                ["en"] = ["light", "switch", "turn on", "turn off", "temperature"],
                ["ru"] = ["свет", "лампа", "розетка", "включи", "выключи", "температура"],
            },
            examples: new Dictionary<string, IReadOnlyList<string>>
            {
                ["en"] = ["turn on kitchen light", "what's the bedroom temperature"],
                ["ru"] = ["включи свет на кухне", "какая температура в гостиной"],
            }));

    public IReadOnlyList<ConfigFieldDescriptor> GetConfigSchema() =>
    [
        new ConfigFieldDescriptor(
            Key: "baseUrl",
            Type: ConfigFieldType.Url,
            Required: true,
            Labels: new Dictionary<string, string>
            {
                ["en"] = "Server URL",
                ["ru"] = "URL сервера",
            },
            Hints: new Dictionary<string, string>
            {
                ["en"] = "Base URL of your Home Assistant instance (e.g. http://homeassistant.local:8123)",
                ["ru"] = "URL экземпляра Home Assistant (например http://homeassistant.local:8123)",
            }),

        new ConfigFieldDescriptor(
            Key: "token",
            Type: ConfigFieldType.Password,
            Required: true,
            Labels: new Dictionary<string, string>
            {
                ["en"] = "Long-Lived Access Token",
                ["ru"] = "Долгосрочный токен доступа",
            },
            Hints: new Dictionary<string, string>
            {
                ["en"] = "Generate in Home Assistant: Profile → Long-Lived Access Tokens",
                ["ru"] = "Создайте в Home Assistant: Профиль → Долгосрочные токены доступа",
            }),

        new ConfigFieldDescriptor(
            Key: "rooms",
            Type: ConfigFieldType.Text,
            Required: false,
            Labels: new Dictionary<string, string>
            {
                ["en"] = "Rooms (comma-separated)",
                ["ru"] = "Комнаты (через запятую)",
            },
            Hints: new Dictionary<string, string>
            {
                ["en"] = "List your home rooms so the assistant can reference them (e.g. kitchen, bedroom, living room)",
                ["ru"] = "Перечислите комнаты, чтобы ассистент мог к ним обращаться (например кухня, спальня, гостиная)",
            }),

        new ConfigFieldDescriptor(
            Key: "defaultRoom",
            Type: ConfigFieldType.Text,
            Required: false,
            Labels: new Dictionary<string, string>
            {
                ["en"] = "Default Room",
                ["ru"] = "Комната по умолчанию",
            },
            Hints: new Dictionary<string, string>
            {
                ["en"] = "Used when no room is mentioned in a command",
                ["ru"] = "Используется, если в команде не указана комната",
            }),
    ];

    public IReadOnlyList<ToolDefinition> GetTools() =>
    [
        new ToolDefinition(Id, "ha.turn_on", "Turn on a Home Assistant entity.",
            """{"type":"object","properties":{"entity_id":{"type":"string"}},"required":["entity_id"]}"""),

        new ToolDefinition(Id, "ha.turn_off", "Turn off a Home Assistant entity.",
            """{"type":"object","properties":{"entity_id":{"type":"string"}},"required":["entity_id"]}"""),

        new ToolDefinition(Id, "ha.get_state", "Get the current state of a Home Assistant entity.",
            """{"type":"object","properties":{"entity_id":{"type":"string"}},"required":["entity_id"]}"""),

        new ToolDefinition(Id, "ha.list_entities",
            "List available Home Assistant entities, optionally filtered by domain (light, switch, climate, sensor).",
            """{"type":"object","properties":{"domain":{"type":"string"}}}""")
    ];

    private static readonly IReadOnlyDictionary<string, (string Rooms, string DefaultRoom)> ContextLabels =
        new Dictionary<string, (string, string)>
        {
            ["en"] = ("Available rooms", "Default room"),
            ["ru"] = ("Комнаты", "Комната по умолчанию"),
        };

    public string GetWorldContextFragment(string userId, string locale)
    {
        if (_worldContext.Rooms.Count == 0)
        {
            return string.Empty;
        }

        var rooms = string.Join(", ", _worldContext.Rooms);
        var defaultRoom = _worldContext.DefaultRoom
            ?? (_worldContext.Rooms.Count > 0 ? _worldContext.Rooms[0] : string.Empty);
        var code = Locale.ToLanguageCode(locale);
        var labels = ContextLabels.TryGetValue(code, out var l) ? l : ContextLabels["en"];
        return $"## Home Assistant\n{labels.Rooms}: {rooms}\n{labels.DefaultRoom}: {defaultRoom}";
    }

    public async Task<Result<string, DomainError>> ExecuteToolAsync(
        string toolName,
        string argsJson,
        CancellationToken ct = default)
    {
        if (_client.HasNoValue)
        {
            return HomeAssistantErrors.NotInitialized();
        }

        var client = _client.Value;

        try
        {
            return toolName switch
            {
                "ha.turn_on" => await CallService(client, "homeassistant", "turn_on", argsJson, ct),
                "ha.turn_off" => await CallService(client, "homeassistant", "turn_off", argsJson, ct),
                "ha.get_state" => await GetState(client, argsJson, ct),
                "ha.list_entities" => await ListEntities(client, argsJson, ct),
                _ => HomeAssistantErrors.UnknownTool(toolName)
            };
        }
        catch (Exception ex)
        {
            return HomeAssistantErrors.RuntimeError(ex.Message);
        }
    }

    public Task<UnitResult<DomainError>> InitializeAsync(
        IReadOnlyDictionary<string, string> config,
        CancellationToken ct = default)
    {
        if (!config.TryGetValue("baseUrl", out var baseUrl) || string.IsNullOrWhiteSpace(baseUrl))
        {
            return Task.FromResult(UnitResult.Failure<DomainError>(HomeAssistantErrors.MissingBaseUrl()));
        }

        if (!config.TryGetValue("token", out var token) || string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(UnitResult.Failure<DomainError>(HomeAssistantErrors.MissingToken()));
        }

        if (config.TryGetValue("rooms", out var roomsStr))
        {
            var rooms = new List<string>(
                roomsStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            config.TryGetValue("defaultRoom", out var defaultRoom);
            _worldContext = new WorldContext(rooms, defaultRoom);
        }

        var http = _httpClientFactory.CreateClient("homeassistant");
        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _client = Maybe.From(new HomeAssistantClient(http));
        return Task.FromResult(UnitResult.Success<DomainError>());
    }

    private static async Task<Result<string, DomainError>> CallService(
        HomeAssistantClient client, string domain, string service, string argsJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var entityId = doc.RootElement.TryGetProperty("entity_id", out var e) ? e.GetString() : null;
        if (string.IsNullOrWhiteSpace(entityId))
        {
            return HomeAssistantErrors.MissingEntityId();
        }

        await client.CallServiceAsync(domain, service, entityId, ct);
        return JsonSerializer.Serialize(new { result = "ok", entity_id = entityId, service });
    }

    private static async Task<Result<string, DomainError>> GetState(
        HomeAssistantClient client, string argsJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var entityId = doc.RootElement.TryGetProperty("entity_id", out var e) ? e.GetString() : null;
        if (string.IsNullOrWhiteSpace(entityId))
        {
            return HomeAssistantErrors.MissingEntityId();
        }

        var state = await client.GetStateAsync(entityId, ct);
        return state ?? JsonSerializer.Serialize(new { error = $"Entity '{entityId}' not found." });
    }

    private static async Task<Result<string, DomainError>> ListEntities(
        HomeAssistantClient client, string argsJson, CancellationToken ct)
    {
        string? domain = null;
        using (var doc = JsonDocument.Parse(argsJson))
        {
            if (doc.RootElement.TryGetProperty("domain", out var d))
            {
                domain = d.GetString();
            }
        }

        var entities = await client.ListEntitiesAsync(domain, ct);
        return JsonSerializer.Serialize(entities);
    }

    private sealed record WorldContext(IReadOnlyList<string> Rooms, string? DefaultRoom);
}
