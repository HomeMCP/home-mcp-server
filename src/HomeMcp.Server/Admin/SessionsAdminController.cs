using HomeMcp.Application.Sessions.Queries.GetSession;
using Microsoft.AspNetCore.Mvc;

namespace HomeMcp.Server.Admin;

/// <summary>
/// Admin REST endpoint for inspecting sessions.
/// Internal use only — not versioned, not exposed externally.
/// </summary>
[ApiController]
[Route("admin/sessions")]
public sealed class SessionsAdminController(GetSessionQueryHandler getSession) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSession(string id, CancellationToken ct)
    {
        var result = await getSession.HandleAsync(new GetSessionQuery(id), ct);

        if (result.IsFailure)
        {
            return NotFound(new { error = result.Error.Message });
        }

        var s = result.Value;
        return Ok(new
        {
            sessionId = s.SessionId,
            userId = s.UserId,
            deviceId = s.DeviceId,
            startedAt = s.StartedAt,
            lastActiveAt = s.LastActiveAt,
            messageCount = s.MessageCount
        });
    }
}
