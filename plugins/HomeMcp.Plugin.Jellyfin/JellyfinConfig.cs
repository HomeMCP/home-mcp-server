namespace HomeMcp.Plugin.Jellyfin;

public sealed record JellyfinConfig(
    string BaseUrl,
    string ApiKey,
    string UserId);
