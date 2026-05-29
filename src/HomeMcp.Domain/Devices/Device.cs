using CSharpFunctionalExtensions;
using HomeMcp.Domain.Devices.Errors;
using HomeMcp.Domain.SharedKernel;
using HomeMcp.Domain.Users;

namespace HomeMcp.Domain.Devices;

public class Device : AggregateRoot<DeviceId>
{
    private readonly TimeProvider _timeProvider;

    private Device(
        DeviceId id,
        UserId primaryUserId,
        DeviceSharingMode sharingMode,
        Maybe<string> location,
        string tokenHash,
        DeviceCapabilities capabilities,
        DateTimeOffset pairedAt,
        Maybe<DateTimeOffset> lastSeenAt,
        TimeProvider timeProvider) : base(id)
    {
        _timeProvider = timeProvider;
        PrimaryUserId = primaryUserId;
        SharingMode = sharingMode;
        Location = location;
        TokenHash = tokenHash;
        Capabilities = capabilities;
        PairedAt = pairedAt;
        LastSeenAt = lastSeenAt;
    }

    public UserId PrimaryUserId { get; private set; }
    public DeviceSharingMode SharingMode { get; }
    public Maybe<string> Location { get; }
    public string TokenHash { get; private set; }
    public DeviceCapabilities Capabilities { get; private set; }
    public DateTimeOffset PairedAt { get; }
    public Maybe<DateTimeOffset> LastSeenAt { get; private set; }

    public static Result<Device, DeviceError> Create(
        UserId primaryUserId,
        string tokenHash,
        DeviceCapabilities capabilities,
        TimeProvider timeProvider,
        DeviceSharingMode sharingMode = DeviceSharingMode.Private,
        Maybe<string> location = default)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            return DeviceErrors.EmptyToken();
        }

        return new Device(
            DeviceId.New(),
            primaryUserId,
            sharingMode,
            location,
            tokenHash,
            capabilities,
            timeProvider.GetUtcNow(),
            Maybe<DateTimeOffset>.None,
            timeProvider);
    }

    public static Device Reconstitute(
        DeviceId id,
        UserId primaryUserId,
        DeviceSharingMode sharingMode,
        Maybe<string> location,
        string tokenHash,
        DeviceCapabilities capabilities,
        DateTimeOffset pairedAt,
        Maybe<DateTimeOffset> lastSeenAt,
        TimeProvider timeProvider) =>
        new(
            id,
            primaryUserId,
            sharingMode,
            location,
            tokenHash,
            capabilities,
            pairedAt,
            lastSeenAt,
            timeProvider);

    public void UpdateToken(string tokenHash) => TokenHash = tokenHash;

    public void UpdateCapabilities(DeviceCapabilities capabilities) => Capabilities = capabilities;

    public void MarkSeen() => LastSeenAt = _timeProvider.GetUtcNow();
}
