using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace HomeMcp.Application.Telemetry;

/// <summary>
/// Central telemetry definitions for the HomeMcp service.
/// ActivitySource and Meter are BCL types — no extra NuGet package is required to use them.
/// The OTel SDK (registered only in HomeMcp.Server) subscribes to these at startup.
/// </summary>
public static class HomeMcpTelemetry
{
    public const string ServiceName = "HomeMcp";

    public static readonly ActivitySource Activities = new(ServiceName, "1.0");
    public static readonly Meter Metrics = new(ServiceName, "1.0");

    public static readonly Counter<long> SessionsStarted =
        Metrics.CreateCounter<long>(
            "homemcp.sessions.started",
            description: "Number of conversation sessions created");

    public static readonly Counter<long> TurnsProcessed =
        Metrics.CreateCounter<long>(
            "homemcp.turns.processed",
            description: "Number of conversation turns fully processed");

    public static readonly Histogram<double> TurnDuration =
        Metrics.CreateHistogram<double>(
            "homemcp.turn.duration",
            unit: "ms",
            description: "End-to-end wall time for a complete conversation turn");

    public static readonly Counter<long> LlmRequests =
        Metrics.CreateCounter<long>(
            "homemcp.llm.requests",
            description: "Number of individual LLM streaming requests sent (one per tool-call iteration)");

    public static readonly Histogram<double> LlmRequestDuration =
        Metrics.CreateHistogram<double>(
            "homemcp.llm.request.duration",
            unit: "ms",
            description: "Wall time for each LLM streaming request to complete");

    public static readonly Counter<long> ToolCalls =
        Metrics.CreateCounter<long>(
            "homemcp.plugin.tool_calls",
            description: "Plugin tool calls invoked by the LLM");

    public static readonly Histogram<double> ToolCallDuration =
        Metrics.CreateHistogram<double>(
            "homemcp.plugin.tool_call.duration",
            unit: "ms",
            description: "Duration of individual plugin tool executions");
}
