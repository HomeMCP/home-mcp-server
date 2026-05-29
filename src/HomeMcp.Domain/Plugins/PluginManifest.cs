using HomeMcp.Domain.SharedKernel;

namespace HomeMcp.Domain.Plugins;

public sealed class PluginManifest : ValueObject
{
    private readonly IReadOnlyDictionary<string, string> _descriptions;

    public PluginManifest(
        string id,
        string version,
        string displayName,
        IReadOnlyDictionary<string, string> descriptions,
        PluginRoutingHints routingHints)
    {
        Id = id;
        Version = version;
        DisplayName = displayName;
        _descriptions = descriptions;
        RoutingHints = routingHints;
    }

    public string Id { get; }
    public string Version { get; }
    public string DisplayName { get; }
    public PluginRoutingHints RoutingHints { get; }

    public string GetDescription(string locale) =>
        _descriptions.TryGetValue(Locale.ToLanguageCode(locale), out var d) ? d
        : _descriptions.Values.FirstOrDefault() ?? string.Empty;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
        yield return Version;
    }
}
