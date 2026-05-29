using Grpc.Core;
using HomeMcp.Application.Orchestration;
using HomeMcp.Application.Sessions.Commands.ProcessTurn;
using HomeMcp.Application.Sessions.Commands.StartSession;
using HomeMcp.Contracts.V1;
using HomeMcp.Domain.Devices;
using AppOrch = HomeMcp.Application.Orchestration;

namespace HomeMcp.Server.GrpcServices;

/// <summary>
/// Handles the single bidirectional streaming RPC — Converse.
/// All other operations (pairing, plugin config, session history) are served via REST.
/// </summary>
public sealed class AssistantService : Assistant.AssistantBase
{
    private const string CodeSessionRequired = "session.required";
    private const string CodeHelloMissingDeviceId = "hello.missing_device_id";
    private const string CodeHelloUnknownDevice = "hello.unknown_device";

    private readonly StartSessionCommandHandler _startSession;
    private readonly ProcessTurnCommandHandler _processTurn;
    private readonly IDeviceRepository _deviceRepo;
    private readonly ILogger<AssistantService> _logger;

    public AssistantService(
        StartSessionCommandHandler startSession,
        ProcessTurnCommandHandler processTurn,
        IDeviceRepository deviceRepo,
        ILogger<AssistantService> logger)
    {
        _startSession = startSession;
        _processTurn = processTurn;
        _deviceRepo = deviceRepo;
        _logger = logger;
    }

    public override async Task Converse(
        IAsyncStreamReader<ClientEvent> requestStream,
        IServerStreamWriter<ServerEvent> responseStream,
        ServerCallContext context)
    {
        string? sessionId = null;

        await foreach (var clientEvent in requestStream.ReadAllAsync(context.CancellationToken))
        {
            switch (clientEvent.PayloadCase)
            {
                case ClientEvent.PayloadOneofCase.Hello:
                    sessionId = await HandleHello(clientEvent.Hello, responseStream, context);
                    break;

                case ClientEvent.PayloadOneofCase.Text:
                    if (sessionId is null)
                    {
                        await responseStream.WriteAsync(
                            MakeError(CodeSessionRequired, "Send Hello first."),
                            context.CancellationToken);
                        break;
                    }

                    await HandleTextInput(sessionId, clientEvent.Text, responseStream, context);
                    break;

                case ClientEvent.PayloadOneofCase.Interrupt:
                    context.CancellationToken.ThrowIfCancellationRequested();
                    break;

                case ClientEvent.PayloadOneofCase.Heartbeat:
                    break;
            }
        }
    }

    private async Task<string?> HandleHello(
        HelloEvent hello,
        IServerStreamWriter<ServerEvent> responseStream,
        ServerCallContext context)
    {
        var ct = context.CancellationToken;

        if (string.IsNullOrWhiteSpace(hello.DeviceId))
        {
            await responseStream.WriteAsync(MakeError(CodeHelloMissingDeviceId, "device_id is required."), ct);
            return null;
        }

        var deviceMaybe = await _deviceRepo.GetByIdAsync(DeviceId.From(hello.DeviceId), ct);
        if (deviceMaybe.HasNoValue)
        {
            _logger.LogWarning("Unknown device tried to connect: {DeviceId}", hello.DeviceId);
            await responseStream.WriteAsync(MakeError(CodeHelloUnknownDevice, "Device not registered. Use POST /api/v1/pair first."), ct);
            return null;
        }

        var device = deviceMaybe.Value;

        var startResult = await _startSession.HandleAsync(
            new StartSessionCommand(device.PrimaryUserId.Value, device.Id.Value),
            ct);

        if (startResult.IsFailure)
        {
            await responseStream.WriteAsync(MakeError(startResult.Error.Code, startResult.Error.Message), ct);
            return null;
        }

        await responseStream.WriteAsync(new ServerEvent
        {
            SessionStarted = new SessionStartedEvent
            {
                SessionId = startResult.Value.SessionId,
                UserId = device.PrimaryUserId.Value
            }
        }, ct);

        return startResult.Value.SessionId;
    }

    private async Task HandleTextInput(
        string sessionId,
        TextInputEvent textInput,
        IServerStreamWriter<ServerEvent> responseStream,
        ServerCallContext context)
    {
        var ct = context.CancellationToken;

        await foreach (var turnEvent in _processTurn.HandleAsync(
            new ProcessTurnCommand(sessionId, textInput.Text), ct))
        {
            var serverEvent = turnEvent switch
            {
                TextChunkEvent chunk => new ServerEvent
                {
                    TextDelta = new TextDeltaEvent { Text = chunk.Text }
                },
                AppOrch.ToolCallStartedEvent start => new ServerEvent
                {
                    ToolCallStarted = new Contracts.V1.ToolCallStartedEvent
                    {
                        PluginId = start.PluginId,
                        ToolName = start.ToolName
                    }
                },
                AppOrch.ToolCallFinishedEvent done => new ServerEvent
                {
                    ToolCallFinished = new Contracts.V1.ToolCallFinishedEvent
                    {
                        PluginId = done.PluginId,
                        ToolName = done.ToolName,
                        ResultJson = done.ResultJson
                    }
                },
                TurnCompletedEvent => new ServerEvent
                {
                    TurnDone = new TurnDoneEvent { Reason = TurnDoneReason.Completed }
                },
                TurnErrorEvent err => MakeError(err.Error.Code, err.Error.Message),
                _ => null
            };

            if (serverEvent is not null)
            {
                await responseStream.WriteAsync(serverEvent, ct);
            }
        }
    }

    private static ServerEvent MakeError(string code, string message) => new()
    {
        Error = new ErrorEvent { Code = code, Message = message }
    };
}
