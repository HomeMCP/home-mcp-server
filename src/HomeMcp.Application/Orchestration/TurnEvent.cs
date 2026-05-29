using HomeMcp.Application.Common.Errors;

namespace HomeMcp.Application.Orchestration;

public abstract record TurnEvent;

public sealed record TextChunkEvent(string Text) : TurnEvent;

public sealed record ToolCallStartedEvent(
    string PluginId,
    string ToolName,
    string ArgsJson) : TurnEvent;

public sealed record ToolCallFinishedEvent(
    string PluginId,
    string ToolName,
    string ResultJson) : TurnEvent;

public sealed record TurnCompletedEvent(string FullResponse) : TurnEvent;

public sealed record TurnErrorEvent(ApplicationError Error) : TurnEvent;
