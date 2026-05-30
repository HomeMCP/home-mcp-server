using System.Text.Json;
using HomeMcp.Application.Orchestration;
using HomeMcp.Application.Sessions.Commands.ProcessTurn;
using HomeMcp.Application.Sessions.Commands.StartSession;
using HomeMcp.Domain.Devices;
using HomeMcp.Server.Admin;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace HomeMcp.Server.Api;

/// <summary>
/// Browser-facing REST endpoint that wraps the gRPC Converse stream as Server-Sent Events.
/// Browsers cannot use bidirectional gRPC streaming natively; this adapter lets the
/// demo Angular client consume the same orchestration pipeline over plain HTTP/2.
/// </summary>
[ApiController]
[Route("api/v1")]
[EnableCors("clients")]
public sealed class ConverseController(
    IDeviceRepository deviceRepo,
    StartSessionCommandHandler startSession,
    ProcessTurnCommandHandler processTurn) : ControllerBase
{
    [HttpPost("converse")]
    public async Task ConverseAsync([FromBody] ConverseRequest req, CancellationToken ct)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        var deviceMaybe = await deviceRepo.GetByIdAsync(DeviceId.From(req.DeviceId), ct);
        if (deviceMaybe.HasNoValue || !CryptoHelper.Verify(req.DeviceToken, deviceMaybe.Value.TokenHash))
        {
            await WriteSse("error", new { code = "auth.failed", message = "Invalid device credentials." }, ct);
            return;
        }

        var device = deviceMaybe.Value;
        string sessionId;

        if (string.IsNullOrEmpty(req.SessionId))
        {
            var startResult = await startSession.HandleAsync(
                new StartSessionCommand(device.PrimaryUserId.Value, device.Id.Value), ct);

            if (startResult.IsFailure)
            {
                await WriteSse("error", new { code = startResult.Error.Code, message = startResult.Error.Message }, ct);
                return;
            }

            sessionId = startResult.Value.SessionId;
            await WriteSse("session_started", new { sessionId, userId = device.PrimaryUserId.Value }, ct);
        }
        else
        {
            sessionId = req.SessionId;
        }

        await foreach (var evt in processTurn.HandleAsync(new ProcessTurnCommand(sessionId, req.Text), ct))
        {
            switch (evt)
            {
                case TextChunkEvent chunk:
                    await WriteSse("text_delta", new { text = chunk.Text }, ct);
                    break;

                case ToolCallStartedEvent start:
                    await WriteSse("tool_call_started", new { pluginId = start.PluginId, toolName = start.ToolName }, ct);
                    break;

                case ToolCallFinishedEvent done:
                    await WriteSse("tool_call_finished", new { pluginId = done.PluginId, toolName = done.ToolName, resultJson = done.ResultJson }, ct);
                    if (done.ToolName.EndsWith(".play_track", StringComparison.OrdinalIgnoreCase))
                    {
                        EmitMediaPlay(done.ResultJson, ct);
                    }

                    break;

                case TurnCompletedEvent completed:
                    await WriteSse("turn_done", new { fullResponse = completed.FullResponse }, ct);
                    break;

                case TurnErrorEvent err:
                    await WriteSse("error", new { code = err.Error.Code, message = err.Error.Message }, ct);
                    break;
            }
        }
    }

    private void EmitMediaPlay(string resultJson, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;
            var url = root.TryGetProperty("url", out var u) ? u.GetString() : null;
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            var title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
            var artist = root.TryGetProperty("artist", out var ar) ? ar.GetString() : null;
            var album = root.TryGetProperty("album", out var al) ? al.GetString() : null;
            var dur = root.TryGetProperty("duration_seconds", out var d) ? (int?)d.GetInt32() : null;

            _ = WriteSse("media_play", new
            {
                url,
                mimeType = "audio/mpeg",
                title,
                artist,
                album,
                durationSeconds = dur,
            }, ct);
        }
        catch (JsonException) { }
    }

    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private async Task WriteSse(string eventType, object data, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(data, CamelCase);
        await Response.WriteAsync($"event: {eventType}\ndata: {json}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}

public sealed record ConverseRequest(
    string DeviceId,
    string DeviceToken,
    string? SessionId,
    string Text);
