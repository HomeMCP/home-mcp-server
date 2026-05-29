using Microsoft.AspNetCore.Mvc;

namespace HomeMcp.Server.Admin;

[ApiController]
[Route("admin/logs")]
public sealed class LogsAdminController(AdminLogBuffer buffer) : ControllerBase
{
    [HttpGet]
    public IActionResult GetLogs(
        [FromQuery] string? source = null,
        [FromQuery] int limit = 100)
    {
        var entries = buffer.GetRecent(Math.Clamp(limit, 1, 500), source);
        return Ok(entries.Select(e => new
        {
            timestamp = e.Timestamp,
            level = e.Level,
            source = e.Source.ToString().ToLowerInvariant(),
            sourceId = e.SourceId,
            message = e.Message
        }));
    }
}
