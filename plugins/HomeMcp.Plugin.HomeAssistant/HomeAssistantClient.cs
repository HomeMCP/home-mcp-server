using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeMcp.Plugin.HomeAssistant;

public sealed class HomeAssistantClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public HomeAssistantClient(HttpClient http) => _http = http;

    public async Task CallServiceAsync(
        string domain,
        string service,
        string entityId,
        CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new { entity_id = entityId });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync($"api/services/{domain}/{service}", content, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string?> GetStateAsync(string entityId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"api/states/{entityId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<IReadOnlyList<EntitySummary>> ListEntitiesAsync(
        string? domain = null,
        CancellationToken ct = default)
    {
        using var response = await _http.GetAsync("api/states", ct);
        response.EnsureSuccessStatusCode();

        var states = await response.Content.ReadFromJsonAsync<List<HaState>>(JsonOpts, ct) ?? [];

        return states
            .Where(s => domain is null || s.EntityId?.StartsWith(domain + ".", StringComparison.OrdinalIgnoreCase) == true)
            .Select(s => new EntitySummary(
                s.EntityId ?? string.Empty,
                s.State ?? string.Empty,
                s.Attributes?.TryGetValue("friendly_name", out var fn) == true ? fn?.ToString() ?? string.Empty : string.Empty))
            .ToList();
    }

    private sealed class HaState
    {
        [JsonPropertyName("entity_id")] public string? EntityId { get; set; }
        [JsonPropertyName("state")] public string? State { get; set; }
        [JsonPropertyName("attributes")] public Dictionary<string, object?>? Attributes { get; set; }
    }
}

public sealed record EntitySummary(string EntityId, string State, string FriendlyName);
