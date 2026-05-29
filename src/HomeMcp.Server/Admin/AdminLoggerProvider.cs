namespace HomeMcp.Server.Admin;

public sealed class AdminLoggerProvider(AdminLogBuffer buffer) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new AdminLogger(buffer, categoryName);

    public void Dispose() { }
}

internal sealed class AdminLogger(AdminLogBuffer buffer, string category) : ILogger
{
    private static readonly string[] LlmCategories =
    [
        "OllamaChatOrchestrator", "IChatClient", "OllamaSharp"
    ];

    private static readonly string[] PluginCategories =
    [
        "PluginInitializationService", "HomeMcp.Plugin"
    ];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        if (exception is not null)
        {
            message += $"\n{exception}";
        }

        var source = ResolveSource(category, out var sourceId);
        buffer.Add(new AdminLogEntry(
            DateTimeOffset.UtcNow,
            logLevel.ToString().ToLowerInvariant(),
            source,
            sourceId,
            message));
    }

    private static LogSource ResolveSource(string cat, out string id)
    {
        foreach (var prefix in LlmCategories)
        {
            if (cat.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            {
                id = "llm";
                return LogSource.Llm;
            }
        }

        foreach (var prefix in PluginCategories)
        {
            if (cat.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            {
                id = cat.Split('.').LastOrDefault() ?? cat;
                return LogSource.Plugin;
            }
        }

        id = "application";
        return LogSource.Application;
    }
}
