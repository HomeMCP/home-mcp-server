using CSharpFunctionalExtensions;
using HomeMcp.Domain.Devices;
using HomeMcp.Domain.Sessions.Errors;
using HomeMcp.Domain.SharedKernel;
using HomeMcp.Domain.Users;

namespace HomeMcp.Domain.Sessions;

public class Session : AggregateRoot<SessionId>
{
    private readonly TimeProvider _timeProvider;
    private readonly List<Message> _messages = [];

    private Session(
        SessionId id,
        UserId userId,
        DeviceId deviceId,
        string locale,
        DateTimeOffset startedAt,
        DateTimeOffset lastActiveAt,
        TimeProvider timeProvider) : base(id)
    {
        _timeProvider = timeProvider;
        UserId = userId;
        DeviceId = deviceId;
        Locale = locale;
        StartedAt = startedAt;
        LastActiveAt = lastActiveAt;
    }

    public UserId UserId { get; }
    public DeviceId DeviceId { get; }
    public string Locale { get; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset LastActiveAt { get; private set; }
    public Maybe<string> Summary { get; private set; }
    public Maybe<long> SummaryUntilMessageId { get; private set; }
    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

    public static Result<Session, SessionError> Create(
        UserId userId,
        DeviceId deviceId,
        TimeProvider timeProvider,
        string locale = "ru-RU")
    {
        if (userId == default || string.IsNullOrWhiteSpace(userId.Value))
        {
            return SessionErrors.InvalidUser();
        }

        if (deviceId == default || string.IsNullOrWhiteSpace(deviceId.Value))
        {
            return SessionErrors.InvalidDevice();
        }

        if (string.IsNullOrWhiteSpace(locale))
        {
            return SessionErrors.InvalidLocale();
        }

        var now = timeProvider.GetUtcNow();
        return new Session(SessionId.New(), userId, deviceId, locale, now, now, timeProvider);
    }

    public static Session Reconstitute(
        SessionId id,
        UserId userId,
        DeviceId deviceId,
        string locale,
        DateTimeOffset startedAt,
        DateTimeOffset lastActiveAt,
        Maybe<string> summary,
        Maybe<long> summaryUntilMessageId,
        IEnumerable<Message> messages,
        TimeProvider timeProvider)
    {
        var session = new Session(id, userId, deviceId, locale, startedAt, lastActiveAt, timeProvider)
        {
            Summary = summary,
            SummaryUntilMessageId = summaryUntilMessageId
        };
        session._messages.AddRange(messages);
        return session;
    }

    public void AddMessage(Message message)
    {
        _messages.Add(message);
        LastActiveAt = _timeProvider.GetUtcNow();
    }

    public void Touch() => LastActiveAt = _timeProvider.GetUtcNow();

    public void UpdateSummary(string summary, long untilMessageId)
    {
        Summary = Maybe.From(summary);
        SummaryUntilMessageId = Maybe.From(untilMessageId);
    }

    public bool IsExpired(TimeSpan timeout) =>
        _timeProvider.GetUtcNow() - LastActiveAt > timeout;

    public IReadOnlyList<Message> GetMessagesAfterSummary() =>
        SummaryUntilMessageId.HasValue
            ? _messages.Where(m => m.Id.Value > SummaryUntilMessageId.Value).ToList()
            : _messages;
}
