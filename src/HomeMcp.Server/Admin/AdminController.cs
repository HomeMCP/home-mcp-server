using Microsoft.AspNetCore.Mvc;

namespace HomeMcp.Server.Admin;

[ApiController]
[Route("admin")]
public sealed class AdminController(SetupState setup) : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() =>
        Ok(new
        {
            status = "ok",
            version = "0.1.0",
            uptime = (long)(DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds
        });

    [HttpGet("setup/status")]
    public IActionResult SetupStatus() =>
        Ok(new { complete = setup.IsComplete, serverName = setup.ServerName });

    [HttpPost("setup/init")]
    public IActionResult SetupInit([FromBody] SetupInitRequest req)
    {
        if (setup.IsComplete)
        {
            return Conflict(new { error = "Setup already completed." });
        }

        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8)
        {
            return BadRequest(new { error = "Password must be at least 8 characters." });
        }

        setup.Complete(
            req.ServerName ?? "Home MCP",
            req.DefaultLocale ?? "en-US",
            req.Password);

        return Ok(new { ok = true });
    }

    // Generates a one-time 6-character PIN a device must present at POST /api/v1/pair.
    // PIN expires after 10 minutes. Only one outstanding PIN exists at a time.
    [HttpPost("pairing/pin")]
    public IActionResult GeneratePairingPin()
    {
        if (!setup.IsComplete)
        {
            return StatusCode(503, new { error = "Server setup not complete. Run POST /admin/setup/init first." });
        }

        var pin = setup.GeneratePairingPin();
        return Ok(new { pin, expiresInSeconds = 600 });
    }
}

public sealed record SetupInitRequest(
    string Password,
    string? ServerName,
    string? DefaultLocale);
