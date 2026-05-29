using System.Net.Http.Headers;
using System.Text.Json;
using CSharpFunctionalExtensions;
using HomeMcp.Domain.Plugins;
using HomeMcp.Domain.SharedKernel.Errors;
using HomeMcp.Plugin.Jellyfin.Errors;

namespace HomeMcp.Plugin.Jellyfin;

public sealed class JellyfinPlugin : IPlugin
{
    private readonly IHttpClientFactory _httpClientFactory;
    private Maybe<JellyfinClient> _client = Maybe<JellyfinClient>.None;

    public JellyfinPlugin(IHttpClientFactory httpClientFactory) =>
        _httpClientFactory = httpClientFactory;

    public string Id => "io.jellyfin";

    public PluginManifest Manifest { get; } = new(
        id: "io.jellyfin",
        version: "0.1.0",
        displayName: "Jellyfin",
        descriptions: new Dictionary<string, string>
        {
            ["en"] = "Play music and media from your Jellyfin server.",
            ["ru"] = "Воспроизведение музыки и медиа с сервера Jellyfin.",
        },
        routingHints: new PluginRoutingHints(
            descriptions: new Dictionary<string, string>
            {
                ["en"] = "Music playback, searching songs, albums, artists and genres in Jellyfin media server.",
                ["ru"] = "Воспроизведение музыки, поиск песен, альбомов, исполнителей и жанров в Jellyfin.",
            },
            keywords: new Dictionary<string, IReadOnlyList<string>>
            {
                ["en"] = ["play", "music", "song", "track", "album", "artist", "jellyfin", "lyrics", "genre"],
                ["ru"] = ["играй", "музыка", "песня", "трек", "альбом", "исполнитель", "текст", "жанр"],
            },
            examples: new Dictionary<string, IReadOnlyList<string>>
            {
                ["en"] = ["play Pink Floyd", "search for jazz music", "show albums by 50 Cent", "get lyrics"],
                ["ru"] = ["поставь Цой", "найди джаз", "альбомы 50 Cent", "текст песни"],
            }));

    public IReadOnlyList<ConfigFieldDescriptor> GetConfigSchema() =>
    [
        new ConfigFieldDescriptor(
            Key: "baseUrl",
            Type: ConfigFieldType.Url,
            Required: true,
            Labels: new Dictionary<string, string>
            {
                ["en"] = "Server URL",
                ["ru"] = "URL сервера",
            },
            Hints: new Dictionary<string, string>
            {
                ["en"] = "Base URL of your Jellyfin server (e.g. https://jellyfin.local)",
                ["ru"] = "Базовый URL сервера Jellyfin (например https://jellyfin.local)",
            }),

        new ConfigFieldDescriptor(
            Key: "apiKey",
            Type: ConfigFieldType.Password,
            Required: true,
            Labels: new Dictionary<string, string>
            {
                ["en"] = "API Key",
                ["ru"] = "API-ключ",
            },
            Hints: new Dictionary<string, string>
            {
                ["en"] = "API key from Jellyfin Dashboard → API Keys",
                ["ru"] = "API-ключ из раздела Панель управления → API-ключи",
            }),

        new ConfigFieldDescriptor(
            Key: "userId",
            Type: ConfigFieldType.Text,
            Required: false,
            Labels: new Dictionary<string, string>
            {
                ["en"] = "User ID",
                ["ru"] = "ID пользователя",
            },
            Hints: new Dictionary<string, string>
            {
                ["en"] = "Jellyfin user ID for personalised results (optional but recommended)",
                ["ru"] = "ID пользователя Jellyfin для персональных результатов (необязательно, но рекомендуется)",
            }),
    ];

    public IReadOnlyList<ToolDefinition> GetTools() =>
    [
        new ToolDefinition(Id, "jellyfin.search_tracks",
            "Search for music tracks by partial name match. Returns id, title, artist, album, duration_seconds.",
            """{"type":"object","properties":{"query":{"type":"string","description":"Partial track name to search for"},"page":{"type":"integer","default":1,"description":"Page number (25 results per page)"}},"required":["query"]}"""),

        new ToolDefinition(Id, "jellyfin.search_artists",
            "Search for artists by partial name match. Returns artist id and name. Use the artist_id with list_tracks_by_artist or list_albums_by_artist.",
            """{"type":"object","properties":{"query":{"type":"string","description":"Partial artist name to search for"},"page":{"type":"integer","default":1}},"required":["query"]}"""),

        new ToolDefinition(Id, "jellyfin.search_by_genre",
            "Find music tracks by genre (partial name match, e.g. 'hip hop', 'jazz', 'rock'). Returns paginated tracks.",
            """{"type":"object","properties":{"genre":{"type":"string","description":"Genre name or partial name (e.g. hip hop, jazz, classical)"},"page":{"type":"integer","default":1}},"required":["genre"]}"""),

        new ToolDefinition(Id, "jellyfin.list_tracks_by_artist",
            "List all tracks by a specific artist. Use search_artists first to get the artist_id.",
            """{"type":"object","properties":{"artist_id":{"type":"string","description":"Artist ID from search_artists"},"page":{"type":"integer","default":1}},"required":["artist_id"]}"""),

        new ToolDefinition(Id, "jellyfin.list_albums_by_artist",
            "List all albums by a specific artist. Use search_artists first to get the artist_id.",
            """{"type":"object","properties":{"artist_id":{"type":"string","description":"Artist ID from search_artists"},"page":{"type":"integer","default":1}},"required":["artist_id"]}"""),

        new ToolDefinition(Id, "jellyfin.list_tracks_by_album",
            "List all tracks in a specific album. Use list_albums_by_artist to get album_id.",
            """{"type":"object","properties":{"album_id":{"type":"string","description":"Album ID from list_albums_by_artist"},"page":{"type":"integer","default":1}},"required":["album_id"]}"""),

        new ToolDefinition(Id, "jellyfin.play_track",
            "Get a playback stream URL for a track by its item ID. Returns url, title, artist, album, duration_seconds.",
            """{"type":"object","properties":{"item_id":{"type":"string","description":"Track ID from search or list results"}},"required":["item_id"]}"""),

        new ToolDefinition(Id, "jellyfin.get_lyrics",
            "Get the lyrics for a track if available. Returns the full lyrics text.",
            """{"type":"object","properties":{"item_id":{"type":"string","description":"Track ID from search or list results"}},"required":["item_id"]}"""),
    ];

    public string GetWorldContextFragment(string userId, string locale) => string.Empty;

    public async Task<Result<string, DomainError>> ExecuteToolAsync(
        string toolName,
        string argsJson,
        CancellationToken ct = default)
    {
        if (_client.HasNoValue)
        {
            return JellyfinErrors.NotInitialized();
        }

        var client = _client.Value;

        try
        {
            return toolName switch
            {
                "jellyfin.search_tracks" => await SearchTracks(client, argsJson, ct),
                "jellyfin.search_artists" => await SearchArtists(client, argsJson, ct),
                "jellyfin.search_by_genre" => await SearchByGenre(client, argsJson, ct),
                "jellyfin.list_tracks_by_artist" => await ListTracksByArtist(client, argsJson, ct),
                "jellyfin.list_albums_by_artist" => await ListAlbumsByArtist(client, argsJson, ct),
                "jellyfin.list_tracks_by_album" => await ListTracksByAlbum(client, argsJson, ct),
                "jellyfin.play_track" => await PlayTrack(client, argsJson, ct),
                "jellyfin.get_lyrics" => await GetLyrics(client, argsJson, ct),
                _ => JellyfinErrors.UnknownTool(toolName)
            };
        }
        catch (Exception ex)
        {
            return JellyfinErrors.RuntimeError(ex.Message);
        }
    }

    public Task<UnitResult<DomainError>> InitializeAsync(
        IReadOnlyDictionary<string, string> config,
        CancellationToken ct = default)
    {
        if (!config.TryGetValue("baseUrl", out var baseUrl) || string.IsNullOrWhiteSpace(baseUrl))
        {
            return Task.FromResult(UnitResult.Failure<DomainError>(JellyfinErrors.MissingBaseUrl()));
        }

        if (!config.TryGetValue("apiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(UnitResult.Failure<DomainError>(JellyfinErrors.MissingApiKey()));
        }

        config.TryGetValue("userId", out var userId);

        var http = _httpClientFactory.CreateClient("jellyfin");
        http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "MediaBrowser",
            $"Client=\"home-mcp\", Device=\"server\", DeviceId=\"home-mcp-server\", Version=\"1.0.0\", Token={apiKey}");

        _client = Maybe.From(new JellyfinClient(http, userId ?? string.Empty, apiKey));

        return Task.FromResult(UnitResult.Success<DomainError>());
    }

    // ── Tool handlers ─────────────────────────────────────────────────────────

    private static async Task<Result<string, DomainError>> SearchTracks(
        JellyfinClient client, string argsJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var query = doc.RootElement.TryGetProperty("query", out var q) ? q.GetString() ?? string.Empty : string.Empty;
        var page = doc.RootElement.TryGetProperty("page", out var p) ? p.GetInt32() : 1;

        if (string.IsNullOrWhiteSpace(query))
        {
            return JellyfinErrors.EmptyQuery();
        }

        var result = await client.SearchTracksAsync(query, page, ct);
        return SerializePagedTracks(result);
    }

    private static async Task<Result<string, DomainError>> SearchArtists(
        JellyfinClient client, string argsJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var query = doc.RootElement.TryGetProperty("query", out var q) ? q.GetString() ?? string.Empty : string.Empty;
        var page = doc.RootElement.TryGetProperty("page", out var p) ? p.GetInt32() : 1;

        if (string.IsNullOrWhiteSpace(query))
        {
            return JellyfinErrors.EmptyQuery();
        }

        var result = await client.SearchArtistsAsync(query, page, ct);
        return JsonSerializer.Serialize(new
        {
            total = result.Total,
            page = result.Page,
            per_page = result.PageSize,
            artists = result.Items.Select(a => new { id = a.Id, name = a.Name })
        });
    }

    private static async Task<Result<string, DomainError>> SearchByGenre(
        JellyfinClient client, string argsJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var genre = doc.RootElement.TryGetProperty("genre", out var g) ? g.GetString() ?? string.Empty : string.Empty;
        var page = doc.RootElement.TryGetProperty("page", out var p) ? p.GetInt32() : 1;

        if (string.IsNullOrWhiteSpace(genre))
        {
            return JellyfinErrors.EmptyQuery();
        }

        var result = await client.SearchByGenreAsync(genre, page, ct);
        return SerializePagedTracks(result);
    }

    private static async Task<Result<string, DomainError>> ListTracksByArtist(
        JellyfinClient client, string argsJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var artistId = doc.RootElement.TryGetProperty("artist_id", out var id) ? id.GetString() ?? string.Empty : string.Empty;
        var page = doc.RootElement.TryGetProperty("page", out var p) ? p.GetInt32() : 1;

        if (string.IsNullOrWhiteSpace(artistId))
        {
            return JellyfinErrors.EmptyItemId();
        }

        var result = await client.GetTracksByArtistAsync(artistId, page, ct);
        return SerializePagedTracks(result);
    }

    private static async Task<Result<string, DomainError>> ListAlbumsByArtist(
        JellyfinClient client, string argsJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var artistId = doc.RootElement.TryGetProperty("artist_id", out var id) ? id.GetString() ?? string.Empty : string.Empty;
        var page = doc.RootElement.TryGetProperty("page", out var p) ? p.GetInt32() : 1;

        if (string.IsNullOrWhiteSpace(artistId))
        {
            return JellyfinErrors.EmptyItemId();
        }

        var result = await client.GetAlbumsByArtistAsync(artistId, page, ct);
        return JsonSerializer.Serialize(new
        {
            total = result.Total,
            page = result.Page,
            per_page = result.PageSize,
            albums = result.Items.Select(a => new
            {
                id = a.Id,
                name = a.Name,
                artist = a.Artist,
                year = a.Year
            })
        });
    }

    private static async Task<Result<string, DomainError>> ListTracksByAlbum(
        JellyfinClient client, string argsJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var albumId = doc.RootElement.TryGetProperty("album_id", out var id) ? id.GetString() ?? string.Empty : string.Empty;
        var page = doc.RootElement.TryGetProperty("page", out var p) ? p.GetInt32() : 1;

        if (string.IsNullOrWhiteSpace(albumId))
        {
            return JellyfinErrors.EmptyItemId();
        }

        var result = await client.GetTracksByAlbumAsync(albumId, page, ct);
        return SerializePagedTracks(result);
    }

    private static async Task<Result<string, DomainError>> PlayTrack(
        JellyfinClient client, string argsJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var itemId = doc.RootElement.TryGetProperty("item_id", out var id) ? id.GetString() ?? string.Empty : string.Empty;

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return JellyfinErrors.EmptyItemId();
        }

        var track = await client.GetTrackAsync(itemId, ct);
        if (track is null)
        {
            return JellyfinErrors.TrackNotFound(itemId);
        }

        return JsonSerializer.Serialize(new
        {
            url = client.GetStreamUrl(itemId),
            title = track.Title,
            artist = track.Artist,
            album = track.Album,
            duration_seconds = (int)track.Duration.TotalSeconds
        });
    }

    private static async Task<Result<string, DomainError>> GetLyrics(
        JellyfinClient client, string argsJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(argsJson);
        var itemId = doc.RootElement.TryGetProperty("item_id", out var id) ? id.GetString() ?? string.Empty : string.Empty;

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return JellyfinErrors.EmptyItemId();
        }

        var lyrics = await client.GetLyricsAsync(itemId, ct);
        return lyrics is null
            ? JsonSerializer.Serialize(new { available = false, message = "Lyrics not available for this track." })
            : JsonSerializer.Serialize(new { available = true, lyrics });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string SerializePagedTracks(JellyfinPage<JellyfinTrack> page) =>
        JsonSerializer.Serialize(new
        {
            total = page.Total,
            page = page.Page,
            per_page = page.PageSize,
            tracks = page.Items.Select(t => new
            {
                id = t.Id,
                title = t.Title,
                artist = t.Artist,
                album = t.Album,
                duration_seconds = (int)t.Duration.TotalSeconds
            })
        });
}
