using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeMcp.Plugin.Jellyfin;

public sealed class JellyfinClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private const int PageSize = 25;

    private readonly HttpClient _http;
    private readonly string _userId;
    private readonly string _apiKey;

    public JellyfinClient(HttpClient http, string userId, string apiKey)
    {
        _http = http;
        _userId = userId;
        _apiKey = apiKey;
    }

    // ── Search ────────────────────────────────────────────────────────────────

    public async Task<JellyfinPage<JellyfinTrack>> SearchTracksAsync(
        string query,
        int page = 1,
        CancellationToken ct = default)
    {
        var url = BuildUrl("Items",
            ("searchTerm", query),
            ("includeItemTypes", "Audio"),
            ("recursive", "true"),
            ("fields", "Name,AlbumArtist,Album,RunTimeTicks"),
            ("userId", _userId),
            ("limit", PageSize.ToString()),
            ("startIndex", ToStartIndex(page).ToString()),
            ("enableTotalRecordCount", "true"));

        return await FetchPageAsync(url, MapTrack, page, ct);
    }

    public async Task<JellyfinPage<JellyfinArtist>> SearchArtistsAsync(
        string query,
        int page = 1,
        CancellationToken ct = default)
    {
        var url = BuildUrl("Artists/AlbumArtists",
            ("searchTerm", query),
            ("userId", _userId),
            ("limit", PageSize.ToString()),
            ("startIndex", ToStartIndex(page).ToString()),
            ("enableTotalRecordCount", "true"));

        return await FetchPageAsync(url, MapArtist, page, ct);
    }

    public async Task<JellyfinPage<JellyfinTrack>> SearchByGenreAsync(
        string genre,
        int page = 1,
        CancellationToken ct = default)
    {
        var genreIds = await ResolveGenreIdsAsync(genre, ct);
        if (genreIds.Count == 0)
        {
            return JellyfinPages.Empty<JellyfinTrack>(page);
        }

        var url = BuildUrl("Items",
            ("genreIds", string.Join(',', genreIds)),
            ("includeItemTypes", "Audio"),
            ("recursive", "true"),
            ("fields", "Name,AlbumArtist,Album,RunTimeTicks"),
            ("userId", _userId),
            ("sortBy", "Album,IndexNumber"),
            ("limit", PageSize.ToString()),
            ("startIndex", ToStartIndex(page).ToString()),
            ("enableTotalRecordCount", "true"));

        return await FetchPageAsync(url, MapTrack, page, ct);
    }

    // ── Lists ─────────────────────────────────────────────────────────────────

    public async Task<JellyfinPage<JellyfinTrack>> GetTracksByArtistAsync(
        string artistId,
        int page = 1,
        CancellationToken ct = default)
    {
        var url = BuildUrl("Items",
            ("artistIds", artistId),
            ("includeItemTypes", "Audio"),
            ("recursive", "true"),
            ("fields", "Name,AlbumArtist,Album,RunTimeTicks"),
            ("userId", _userId),
            ("sortBy", "Album,IndexNumber"),
            ("limit", PageSize.ToString()),
            ("startIndex", ToStartIndex(page).ToString()),
            ("enableTotalRecordCount", "true"));

        return await FetchPageAsync(url, MapTrack, page, ct);
    }

    public async Task<JellyfinPage<JellyfinAlbum>> GetAlbumsByArtistAsync(
        string artistId,
        int page = 1,
        CancellationToken ct = default)
    {
        var url = BuildUrl("Items",
            ("albumArtistIds", artistId),
            ("includeItemTypes", "MusicAlbum"),
            ("recursive", "true"),
            ("fields", "Name,AlbumArtist,ProductionYear"),
            ("userId", _userId),
            ("sortBy", "ProductionYear,SortName"),
            ("limit", PageSize.ToString()),
            ("startIndex", ToStartIndex(page).ToString()),
            ("enableTotalRecordCount", "true"));

        return await FetchPageAsync(url, MapAlbum, page, ct);
    }

    public async Task<JellyfinPage<JellyfinTrack>> GetTracksByAlbumAsync(
        string albumId,
        int page = 1,
        CancellationToken ct = default)
    {
        var url = BuildUrl("Items",
            ("albumIds", albumId),
            ("includeItemTypes", "Audio"),
            ("recursive", "true"),
            ("fields", "Name,AlbumArtist,Album,RunTimeTicks"),
            ("userId", _userId),
            ("sortBy", "ParentIndexNumber,IndexNumber"),
            ("limit", PageSize.ToString()),
            ("startIndex", ToStartIndex(page).ToString()),
            ("enableTotalRecordCount", "true"));

        return await FetchPageAsync(url, MapTrack, page, ct);
    }

    // ── Single item ───────────────────────────────────────────────────────────

    public async Task<JellyfinTrack?> GetTrackAsync(string itemId, CancellationToken ct = default)
    {
        var url = string.IsNullOrEmpty(_userId)
            ? BuildUrl("Items", ("ids", itemId), ("recursive", "true"), ("fields", "Name,AlbumArtist,Album,RunTimeTicks"))
            : $"Users/{_userId}/Items/{Uri.EscapeDataString(itemId)}";

        using var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        if (string.IsNullOrEmpty(_userId))
        {
            var body = await response.Content.ReadFromJsonAsync<ItemsResult>(JsonOpts, ct);
            return body?.Items?.FirstOrDefault() is { } item ? MapTrack(item) : null;
        }

        var single = await response.Content.ReadFromJsonAsync<JellyfinItem>(JsonOpts, ct);
        return single is null ? null : MapTrack(single);
    }

    public async Task<string?> GetLyricsAsync(string itemId, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"Audio/{Uri.EscapeDataString(itemId)}/Lyrics", ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var dto = await response.Content.ReadFromJsonAsync<LyricDto>(JsonOpts, ct);
        if (dto?.Lyrics is not { Count: > 0 } lines)
        {
            return null;
        }

        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line.Text))
            {
                sb.AppendLine(line.Text);
            }
        }

        return sb.ToString().TrimEnd();
    }

    public string GetStreamUrl(string itemId) =>
        $"{_http.BaseAddress}Audio/{itemId}/universal?UserId={_userId}&api_key={_apiKey}&AudioCodec=mp3";

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<string>> ResolveGenreIdsAsync(string genre, CancellationToken ct)
    {
        var url = BuildUrl("MusicGenres",
            ("searchTerm", genre),
            ("userId", _userId),
            ("limit", "10"));

        using var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var body = await response.Content.ReadFromJsonAsync<ItemsResult>(JsonOpts, ct);
        return body?.Items?
            .Where(i => i.Id is not null)
            .Select(i => i.Id!)
            .ToList() ?? [];
    }

    private async Task<JellyfinPage<T>> FetchPageAsync<T>(
        string url,
        Func<JellyfinItem, T> mapper,
        int page,
        CancellationToken ct)
    {
        using var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ItemsResult>(JsonOpts, ct);
        var items = body?.Items?.Select(mapper).ToList() ?? [];
        return new JellyfinPage<T>(items, body?.TotalRecordCount ?? items.Count, page, PageSize);
    }

    private static string BuildUrl(string path, params (string key, string value)[] query)
    {
        var sb = new StringBuilder(path).Append('?');
        foreach (var (k, v) in query)
        {
            if (!string.IsNullOrEmpty(v))
            {
                sb.Append(Uri.EscapeDataString(k)).Append('=').Append(Uri.EscapeDataString(v)).Append('&');
            }
        }

        return sb.ToString().TrimEnd('&');
    }

    private static int ToStartIndex(int page) => Math.Max(0, page - 1) * PageSize;

    private static JellyfinTrack MapTrack(JellyfinItem i) => new(
        i.Id ?? string.Empty,
        i.Name ?? "Unknown",
        i.AlbumArtist ?? i.Artists?.FirstOrDefault() ?? string.Empty,
        i.Album ?? string.Empty,
        i.RunTimeTicks.HasValue ? TimeSpan.FromTicks(i.RunTimeTicks.Value) : TimeSpan.Zero);

    private static JellyfinArtist MapArtist(JellyfinItem i) =>
        new(i.Id ?? string.Empty, i.Name ?? "Unknown");

    private static JellyfinAlbum MapAlbum(JellyfinItem i) =>
        new(i.Id ?? string.Empty, i.Name ?? "Unknown", i.AlbumArtist ?? string.Empty, i.ProductionYear);

    // ── DTOs ──────────────────────────────────────────────────────────────────

    private sealed class ItemsResult
    {
        [JsonPropertyName("Items")] public List<JellyfinItem>? Items { get; set; }
        [JsonPropertyName("TotalRecordCount")] public int TotalRecordCount { get; set; }
    }

    private sealed class JellyfinItem
    {
        [JsonPropertyName("Id")] public string? Id { get; set; }
        [JsonPropertyName("Name")] public string? Name { get; set; }
        [JsonPropertyName("AlbumArtist")] public string? AlbumArtist { get; set; }
        [JsonPropertyName("Album")] public string? Album { get; set; }
        [JsonPropertyName("RunTimeTicks")] public long? RunTimeTicks { get; set; }
        [JsonPropertyName("ProductionYear")] public int? ProductionYear { get; set; }
        [JsonPropertyName("Artists")] public List<string>? Artists { get; set; }
    }

    private sealed class LyricDto
    {
        [JsonPropertyName("Lyrics")] public List<LyricLine>? Lyrics { get; set; }
    }

    private sealed class LyricLine
    {
        [JsonPropertyName("Text")] public string? Text { get; set; }
    }
}

// ── Public result types ────────────────────────────────────────────────────────

public sealed record JellyfinTrack(string Id, string Title, string Artist, string Album, TimeSpan Duration);

public sealed record JellyfinArtist(string Id, string Name);

public sealed record JellyfinAlbum(string Id, string Name, string Artist, int? Year);

public sealed record JellyfinPage<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public static class JellyfinPages
{
    public static JellyfinPage<T> Empty<T>(int page) => new([], 0, page, 25);
}
