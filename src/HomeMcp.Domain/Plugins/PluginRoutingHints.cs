using HomeMcp.Domain.SharedKernel;

namespace HomeMcp.Domain.Plugins;

public sealed class PluginRoutingHints : ValueObject
{
    private readonly IReadOnlyDictionary<string, string> _descriptions;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _keywords;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _examples;

    public PluginRoutingHints(
        IReadOnlyDictionary<string, string> descriptions,
        IReadOnlyDictionary<string, IReadOnlyList<string>> keywords,
        IReadOnlyDictionary<string, IReadOnlyList<string>> examples)
    {
        _descriptions = descriptions;
        _keywords = keywords;
        _examples = examples;
    }

    public IEnumerable<string> SupportedLocales => _descriptions.Keys;

    public string GetDescription(string locale) =>
        _descriptions.TryGetValue(Locale.ToLanguageCode(locale), out var d) ? d
        : _descriptions.Values.FirstOrDefault() ?? string.Empty;

    public IReadOnlyList<string> GetKeywords(string locale) =>
        _keywords.TryGetValue(Locale.ToLanguageCode(locale), out var k) ? k
        : _keywords.Values.FirstOrDefault() ?? [];

    public IReadOnlyList<string> GetExamples(string locale) =>
        _examples.TryGetValue(Locale.ToLanguageCode(locale), out var e) ? e
        : _examples.Values.FirstOrDefault() ?? [];

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var (code, desc) in _descriptions.OrderBy(kv => kv.Key))
        {
            yield return $"{code}:{desc}";
        }
    }
}
