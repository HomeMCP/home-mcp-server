using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HomeMcp.Application.Common.Errors;
using HomeMcp.Application.Orchestration;
using HomeMcp.Application.Plugins;
using HomeMcp.Application.Telemetry;
using HomeMcp.Domain.Plugins;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace HomeMcp.Infrastructure.LlmClient;

public sealed class OllamaChatOrchestrator : IChatOrchestrator
{
    private readonly IChatClient _chatClient;
    private readonly IPluginRegistry _registry;
    private readonly ILogger<OllamaChatOrchestrator> _logger;
    private readonly object _cacheLock = new();
    private ToolIndexCache? _cache;

    public OllamaChatOrchestrator(
        IChatClient chatClient,
        IPluginRegistry registry,
        ILogger<OllamaChatOrchestrator> logger)
    {
        _chatClient = chatClient;
        _registry = registry;
        _logger = logger;
    }

    public async IAsyncEnumerable<TurnEvent> OrchestrateAsync(
        OrchestratorContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var messages = BuildMessages(context);
        var cache = GetOrRebuildCache(context.Plugins, _registry.RegistryVersion);
        var options = new ChatOptions { Tools = cache.Tools };

        const int maxIterations = 10;
        var iteration = 0;

        while (iteration++ < maxIterations)
        {
            var pendingToolCalls = new List<FunctionCallContent>();
            var textAccumulator = new StringBuilder();

            HomeMcpTelemetry.LlmRequests.Add(1);
            var llmSw = Stopwatch.StartNew();

            using var llmActivity = HomeMcpTelemetry.Activities.StartActivity(
                "llm.chat",
                ActivityKind.Client);
            llmActivity?.SetTag("llm.iteration", iteration);

            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, options, ct))
            {
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case TextContent text when !string.IsNullOrEmpty(text.Text):
                            textAccumulator.Append(text.Text);
                            yield return new TextChunkEvent(text.Text);
                            break;

                        case FunctionCallContent call:
                            pendingToolCalls.Add(call);
                            break;
                    }
                }
            }

            llmSw.Stop();
            HomeMcpTelemetry.LlmRequestDuration.Record(llmSw.Elapsed.TotalMilliseconds,
                new TagList { { "llm.iteration", iteration } });
            llmActivity?.SetTag("llm.tool_calls_requested", pendingToolCalls.Count);

            var assistantContents = new List<AIContent>();
            if (textAccumulator.Length > 0)
            {
                assistantContents.Add(new TextContent(textAccumulator.ToString()));
            }

            assistantContents.AddRange(pendingToolCalls.Cast<AIContent>());
            messages.Add(new ChatMessage(ChatRole.Assistant, assistantContents));

            if (pendingToolCalls.Count == 0)
            {
                yield return new TurnCompletedEvent(textAccumulator.ToString());
                yield break;
            }

            foreach (var call in pendingToolCalls)
            {
                if (!cache.Lookup.TryGetValue(call.Name, out var entry))
                {
                    _logger.LogWarning("LLM called unknown tool '{ToolName}'", call.Name);
                    messages.Add(new ChatMessage(ChatRole.Tool, [
                        new FunctionResultContent(call.CallId, "Error: tool not found.")
                    ]));
                    continue;
                }

                var argsJson = call.Arguments is not null
                    ? JsonSerializer.Serialize(call.Arguments)
                    : "{}";

                yield return new ToolCallStartedEvent(entry.Plugin.Id, call.Name, argsJson);

                HomeMcpTelemetry.ToolCalls.Add(1, new TagList
                {
                    { "plugin.id", entry.Plugin.Id },
                    { "tool.name", call.Name }
                });

                var toolSw = Stopwatch.StartNew();
                using var toolActivity = HomeMcpTelemetry.Activities.StartActivity(
                    "plugin.tool_call",
                    ActivityKind.Internal);
                toolActivity?.SetTag("plugin.id", entry.Plugin.Id);
                toolActivity?.SetTag("tool.name", call.Name);

                var toolResult = await entry.Plugin.ExecuteToolAsync(call.Name, argsJson, ct);
                toolSw.Stop();

                HomeMcpTelemetry.ToolCallDuration.Record(toolSw.Elapsed.TotalMilliseconds, new TagList
                {
                    { "plugin.id", entry.Plugin.Id },
                    { "tool.name", call.Name }
                });

                var resultText = toolResult.IsSuccess
                    ? toolResult.Value
                    : $"Error: {toolResult.Error?.Message}";

                toolActivity?.SetTag("tool.success", toolResult.IsSuccess);

                yield return new ToolCallFinishedEvent(entry.Plugin.Id, call.Name, resultText);

                messages.Add(new ChatMessage(ChatRole.Tool, [
                    new FunctionResultContent(call.CallId, resultText)
                ]));
            }
        }

        yield return new TurnErrorEvent(
            ApplicationError.Llm("llm.max_iterations", "Maximum tool call iterations exceeded."));
    }

    private ToolIndexCache GetOrRebuildCache(IReadOnlyList<IPlugin> plugins, string registryVersion)
    {
        lock (_cacheLock)
        {
            if (_cache is not null && _cache.RegistryVersion == registryVersion)
            {
                return _cache;
            }

            _logger.LogInformation("Rebuilding tool index cache (version '{Version}').", registryVersion);
            _cache = BuildCache(plugins, registryVersion);
            return _cache;
        }
    }

    private static ToolIndexCache BuildCache(IReadOnlyList<IPlugin> plugins, string registryVersion)
    {
        var lookup = new Dictionary<string, PluginToolEntry>(StringComparer.OrdinalIgnoreCase);
        var tools = new List<AITool>();

        foreach (var plugin in plugins)
        {
            foreach (var tool in plugin.GetTools())
            {
                var aiFunction = new PluginToolFunction(plugin, tool);
                lookup[tool.Name] = new PluginToolEntry(plugin, tool);
                tools.Add(aiFunction);
            }
        }

        return new ToolIndexCache(registryVersion, lookup, tools);
    }

    private static List<ChatMessage> BuildMessages(OrchestratorContext context)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, context.SystemPrompt)
        };
        foreach (var h in context.History)
        {
            messages.Add(new ChatMessage(new ChatRole(h.Role), h.Content));
        }

        messages.Add(new ChatMessage(ChatRole.User, context.UserInput));
        return messages;
    }
}
