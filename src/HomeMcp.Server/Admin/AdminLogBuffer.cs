using System.Collections.Concurrent;

namespace HomeMcp.Server.Admin;

public sealed class AdminLogBuffer
{
    private const int Capacity = 500;
    private readonly ConcurrentQueue<AdminLogEntry> _queue = new();

    public void Add(AdminLogEntry entry)
    {
        _queue.Enqueue(entry);
        while (_queue.Count > Capacity)
        {
            _queue.TryDequeue(out _);
        }
    }

    public IReadOnlyList<AdminLogEntry> GetRecent(int limit = 100, string? source = null)
    {
        var all = _queue.ToArray();
        var filtered = source switch
        {
            "plugin" => all.Where(e => e.Source == LogSource.Plugin),
            "llm" => all.Where(e => e.Source == LogSource.Llm),
            "application" => all.Where(e => e.Source == LogSource.Application),
            _ => all.AsEnumerable()
        };
        return filtered.OrderByDescending(e => e.Timestamp).Take(limit).ToList();
    }
}
