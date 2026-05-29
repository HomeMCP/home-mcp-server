using CSharpFunctionalExtensions;
using HomeMcp.Domain.SharedKernel.Errors;

namespace HomeMcp.Domain.Plugins;

public interface IPlugin
{
    string Id { get; }
    PluginManifest Manifest { get; }

    IReadOnlyList<ToolDefinition> GetTools();

    IReadOnlyList<ConfigFieldDescriptor> GetConfigSchema();

    string GetWorldContextFragment(string userId, string locale);

    Task<Result<string, DomainError>> ExecuteToolAsync(
        string toolName,
        string argsJson,
        CancellationToken ct = default);

    Task<UnitResult<DomainError>> InitializeAsync(
        IReadOnlyDictionary<string, string> config,
        CancellationToken ct = default);
}
