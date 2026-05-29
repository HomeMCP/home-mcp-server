using CSharpFunctionalExtensions;

namespace HomeMcp.Domain.Plugins;

public enum ConfigFieldType
{
    Text,
    Password,
    Url,
    Integer,
    Boolean,
    Select,
}

public sealed record ConfigFieldDescriptor(
    string Key,
    ConfigFieldType Type,
    bool Required,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyDictionary<string, string> Hints,
    Maybe<string> DefaultValue = default,
    Maybe<IReadOnlyList<string>> Options = default);
