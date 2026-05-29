using HomeMcp.Application.Plugins;
using HomeMcp.Domain.Plugins;
using Microsoft.AspNetCore.Mvc;

namespace HomeMcp.Server.Admin;

[ApiController]
[Route("admin/plugins")]
public sealed class PluginsAdminController(
    IEnumerable<IPlugin> plugins,
    IPluginRegistry registry) : ControllerBase
{
    [HttpGet]
    public IActionResult List()
    {
        var result = plugins.Select(p => MapPlugin(p, registry)).ToList();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public IActionResult Get(string id)
    {
        var plugin = plugins.FirstOrDefault(p =>
            string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

        if (plugin is null)
        {
            return NotFound(new { error = $"Plugin '{id}' not found." });
        }

        return Ok(MapPlugin(plugin, registry));
    }

    private static object MapPlugin(IPlugin p, IPluginRegistry reg)
    {
        var active = reg.GetAll().Any(rp =>
            string.Equals(rp.Id, p.Id, StringComparison.OrdinalIgnoreCase));

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
            configSchema = p.GetConfigSchema().Select(f => new
            {
                key = f.Key,
                type = f.Type.ToString().ToLowerInvariant(),
                required = f.Required,
                labels = f.Labels,
                hints = f.Hints,
                defaultValue = f.DefaultValue.HasValue ? f.DefaultValue.Value : null,
                options = f.Options.HasValue ? f.Options.Value : null
            })
        };
    }
}
