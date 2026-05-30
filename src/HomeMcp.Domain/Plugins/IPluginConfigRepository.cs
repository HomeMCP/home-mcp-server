using CSharpFunctionalExtensions;
using HomeMcp.Domain.SharedKernel.Errors;

namespace HomeMcp.Domain.Plugins;

public interface IPluginConfigRepository
{
    Task<IReadOnlyDictionary<string, string>> GetConfigAsync(
        string pluginId,
        CancellationToken ct = default);

    Task<UnitResult<DomainError>> ReplaceConfigAsync(
        string pluginId,
        IReadOnlyDictionary<string, string> config,
        CancellationToken ct = default);
}
