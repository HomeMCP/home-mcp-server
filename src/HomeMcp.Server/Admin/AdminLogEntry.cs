namespace HomeMcp.Server.Admin;

public sealed record AdminLogEntry(
    DateTimeOffset Timestamp,
    string Level,
    LogSource Source,
    string SourceId,
    string Message);
