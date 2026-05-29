using CSharpFunctionalExtensions;
using HomeMcp.Domain.Plugins;
using HomeMcp.Domain.SharedKernel.Errors;

namespace HomeMcp.Application.Plugins;

public interface IPluginRegistry
{
    IReadOnlyList<IPlugin> GetAll();

    Maybe<IPlugin> GetById(string pluginId);

    UnitResult<DomainError> Register(IPlugin plugin);

    /// Version tag that changes whenever a plugin is registered or its manifest version changes.
    /// Consumers use this to detect stale caches.
    string RegistryVersion { get; }
}
