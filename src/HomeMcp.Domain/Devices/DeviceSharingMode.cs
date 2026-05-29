namespace HomeMcp.Domain.Devices;

#pragma warning disable CA1028 // Backing type is long to keep sqlite mapping stable.
public enum DeviceSharingMode : long
#pragma warning restore CA1028
{
    Private,
    Shared,
}
