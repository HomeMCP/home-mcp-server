using HomeMcp.Domain.SharedKernel;

namespace HomeMcp.Domain.Devices;

public sealed class DeviceCapabilities : ValueObject
{
    public DeviceCapabilities
    (
        bool hasStt,
        bool hasTts,
        bool hasAudioPlayback,
        bool hasMediaPlayer,
        bool hasDisplay,
        bool hasWakeWord
    )
    {
        HasStt = hasStt;
        HasTts = hasTts;
        HasAudioPlayback = hasAudioPlayback;
        HasMediaPlayer = hasMediaPlayer;
        HasDisplay = hasDisplay;
        HasWakeWord = hasWakeWord;
    }

    public bool HasStt { get; }
    public bool HasTts { get; }
    public bool HasAudioPlayback { get; }
    public bool HasMediaPlayer { get; }
    public bool HasDisplay { get; }
    public bool HasWakeWord { get; }

    public static DeviceCapabilities TextOnly() =>
        new(false, false, false, false, true, false);

    public static DeviceCapabilities Full() =>
        new(true, true, true, true, true, false);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return HasStt;
        yield return HasTts;
        yield return HasAudioPlayback;
        yield return HasMediaPlayer;
        yield return HasDisplay;
        yield return HasWakeWord;
    }
}
