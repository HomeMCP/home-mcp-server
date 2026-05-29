using CSharpFunctionalExtensions;
using HomeMcp.Domain.Plugins;

namespace HomeMcp.Application.Orchestration;

public sealed record OrchestratorContext(
    string SystemPrompt,
    IReadOnlyList<HistoryMessage> History,
    string UserInput,
    IReadOnlyList<IPlugin> Plugins);

public sealed record HistoryMessage(
    string Role,
    string Content,
    Maybe<string> ToolCallsJson = default);
