using CSharpFunctionalExtensions;
using HomeMcp.Application.Common;
using HomeMcp.Application.Common.Errors;
using HomeMcp.Domain.Devices;
using HomeMcp.Domain.Users;
using HomeMcp.Server.Admin;
using Microsoft.AspNetCore.Mvc;
using DomainUser = HomeMcp.Domain.Users.User;

namespace HomeMcp.Server.Api;

/// <summary>
/// External-facing REST endpoint for one-time device pairing.
/// Flow:
///   1. Admin calls POST /admin/pairing/pin  → receives a 6-char PIN (10 min TTL)
///   2. Admin shows PIN to the user
///   3. Device sends POST /api/v1/pair with the PIN
///   4. Server validates PIN, creates a User + Device, returns deviceId + token
///   5. Device stores deviceId + token and presents them in HelloEvent on each gRPC connect
/// </summary>
[ApiController]
[Route("api/v1")]
[Microsoft.AspNetCore.Cors.EnableCors("clients")]
public sealed class PairingController(
    SetupState setup,
    IUserRepository userRepo,
    IDeviceRepository deviceRepo,
    IUnitOfWork uow,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpPost("pair")]
    public async Task<IActionResult> Pair([FromBody] PairRequest req, CancellationToken ct)
    {
        if (!setup.IsComplete)
        {
            return StatusCode(503, new { error = "Server setup not complete." });
        }

        if (!setup.VerifyAndConsumePairingPin(req.Pin))
        {
            return Unauthorized(new { error = "Invalid or expired pairing PIN." });
        }

        var locale = req.Locale ?? setup.DefaultLocale;
        var displayName = req.DisplayName ?? "Home User";

        var userResult = DomainUser.Create(displayName, timeProvider, locale);
        if (userResult.IsFailure)
        {
            return BadRequest(new { error = userResult.Error.Message });
        }

        var user = userResult.Value;
        var plainToken = CryptoHelper.GenerateToken();
        var tokenHash = CryptoHelper.Hash(plainToken);

        var caps = req.Capabilities;
        var deviceResult = Device.Create(
            user.Id,
            tokenHash,
            new DeviceCapabilities(
                caps?.Stt ?? false,
                caps?.Tts ?? false,
                caps?.AudioPlayback ?? false,
                caps?.MediaPlayer ?? false,
                caps?.Display ?? true,
                caps?.WakeWord ?? false),
            timeProvider);

        if (deviceResult.IsFailure)
        {
            return BadRequest(new { error = deviceResult.Error.Message });
        }

        var device = deviceResult.Value;

        var saveResult = await uow.ExecuteAsync(async saveCt =>
        {
            var userAdd = await userRepo.AddAsync(user, saveCt);
            if (userAdd.IsFailure)
            {
                return UnitResult.Failure(userAdd.Error.ToApplicationError());
            }

            var deviceAdd = await deviceRepo.AddAsync(device, saveCt);
            return deviceAdd.IsFailure
                ? UnitResult.Failure(deviceAdd.Error.ToApplicationError())
                : UnitResult.Success<ApplicationError>();
        }, ct);

        if (saveResult.IsFailure)
        {
            return StatusCode(500, new { error = saveResult.Error.Message });
        }

        return Ok(new PairResponse(device.Id.Value, plainToken));
    }
}

public sealed record PairRequest(
    string? Pin,
    string? DisplayName,
    string? Locale,
    PairCapabilities? Capabilities);

public sealed record PairCapabilities(
    bool Stt,
    bool Tts,
    bool AudioPlayback,
    bool MediaPlayer,
    bool Display,
    bool WakeWord);

public sealed record PairResponse(
    string DeviceId,
    string DeviceToken);
