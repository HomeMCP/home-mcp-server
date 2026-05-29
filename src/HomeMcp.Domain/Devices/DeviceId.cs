namespace HomeMcp.Domain.Devices;

public readonly record struct DeviceId(string Value) : IEquatable<DeviceId>
{
    public static DeviceId New() => new(Guid.CreateVersion7().ToString("N"));
    public static DeviceId From(string value) => new(value);
    public override string ToString() => Value;
}
