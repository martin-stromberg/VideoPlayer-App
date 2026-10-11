using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VideoPlayer.ApiClient.Models;

namespace VideoPlayer.ApiClient;

/// <summary>
/// Plattformneutraler Client für die öffentliche HTTP-API der Serveranwendung
/// „VideoPlayer". Kapselt das Client-Gate (<see cref="ClientGate.HeaderName"/>),
/// die tokenbasierte Anmeldung sowie die feste Wiederholungsstrategie:
/// ein Initialversuch plus bis zu drei Wiederholungen mit den Warteintervallen
/// 1 s, 2 s und 4 s. Wiederholt werden Serverfehler (5xx), Transportfehler,
/// Timeouts sowie 408 und 429; übrige 4xx-Fehler nicht. Auf 401 folgt eine
/// einmalige Token-Erneuerung über <see cref="VideoPlayerApiClientOptions.TokenRefresher"/>.
/// </summary>
public class VideoPlayerApiClient
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4)
    ];

    private const int MaxRetries = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient httpClient;
    private readonly VideoPlayerApiClientOptions options;
    private readonly SemaphoreSlim tokenRefreshGate = new(1, 1);

    /// <summary>
    /// Erstellt den Client. Übernimmt <see cref="VideoPlayerApiClientOptions.BaseAddress"/>
    /// auf den <see cref="HttpClient"/>, sofern dort noch keine
    /// <see cref="HttpClient.BaseAddress"/> gesetzt ist, und ergänzt den
    /// Client-Gate-Header (<see cref="ClientGate.HeaderName"/>), sofern noch
    /// nicht vorhanden.
    /// </summary>
    /// <param name="httpClient">Zu verwendender HTTP-Client.</param>
    /// <param name="options">Optionale Konfiguration (API-Schlüssel, Token-Erneuerung, Verzögerung, Logger).</param>
    /// <exception cref="ArgumentNullException"><paramref name="httpClient"/> ist <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Keine Server-Basisadresse verfügbar.</exception>
    public VideoPlayerApiClient(HttpClient httpClient, VideoPlayerApiClientOptions? options = null)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.options = options ?? new VideoPlayerApiClientOptions();

        if (this.options.BaseAddress is not null && httpClient.BaseAddress is null)
        {
            httpClient.BaseAddress = this.options.BaseAddress;
        }

        if (httpClient.BaseAddress is null)
        {
            throw new InvalidOperationException(
                $"Eine Server-Basisadresse ist erforderlich ({nameof(HttpClient.BaseAddress)} oder {nameof(VideoPlayerApiClientOptions)}.{nameof(VideoPlayerApiClientOptions.BaseAddress)}).");
        }

        if (!httpClient.DefaultRequestHeaders.Contains(ClientGate.HeaderName))
        {
            var apiKey = string.IsNullOrWhiteSpace(this.options.ApiKey)
                ? ClientGate.Resolve()
                : this.options.ApiKey;
            httpClient.DefaultRequestHeaders.Add(ClientGate.HeaderName, apiKey);
        }
    }

    /// <summary>Aktuell gesetztes Bearer-Token, falls vorhanden.</summary>
    public string? CurrentToken => httpClient.DefaultRequestHeaders.Authorization?.Parameter;

    /// <summary>Setzt das Bearer-Token für Folgeanfragen.</summary>
    /// <param name="token">Zu setzendes Token.</param>
    public void SetAccessToken(AuthToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Token);
    }

    /// <summary>Entfernt das gesetzte Bearer-Token.</summary>
    public void ClearAccessToken()
    {
        httpClient.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Ersetzt den Client-Gate-Schlüssel (<see cref="ClientGate.HeaderName"/>) für
    /// Folgeanfragen — etwa nach einem erfolgreichen Geräte-Pairing oder dem
    /// Rückfall auf einen statischen Schlüssel.
    /// </summary>
    /// <param name="apiKey">Der neue Gate-Schlüssel.</param>
    /// <exception cref="ArgumentException"><paramref name="apiKey"/> ist leer.</exception>
    public void SetApiKey(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        httpClient.DefaultRequestHeaders.Remove(ClientGate.HeaderName);
        httpClient.DefaultRequestHeaders.Add(ClientGate.HeaderName, apiKey);
    }

    #region Authentication

    /// <summary>
    /// Meldet den Benutzer am Server an (<c>POST api/auth/login</c>) und setzt
    /// das erhaltene Token für Folgeanfragen.
    /// </summary>
    /// <param name="email">E-Mail-Adresse des Benutzers.</param>
    /// <param name="password">Passwort des Benutzers.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Das erhaltene Anmelde-Token.</returns>
    public async Task<AuthToken> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var token = await PostJsonAsync<AuthToken>(
            "api/auth/login",
            new LoginRequest { Email = email, Password = password },
            cancellationToken,
            allowReauth: false);

        SetAccessToken(token);
        return token;
    }

    /// <summary>
    /// Erneuert die Sitzung über den Refresh-Token
    /// (<c>POST api/auth/refresh</c>): der Server rotiert den Refresh-Token
    /// atomar — die Antwort enthält das neue Anmelde-Token und den
    /// Nachfolge-Refresh-Token. Der Endpunkt liegt hinter dem Client-Gate;
    /// der Header wird wie bei allen Anfragen dieses Clients mitgesendet.
    /// Keine 401-Erneuerung auf diesem Aufruf (der Aufruf <b>ist</b> die
    /// Erneuerung).
    /// </summary>
    /// <param name="refreshToken">Der aktuell gültige Refresh-Token.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die erneuerte Sitzung (Anmelde-Token + rotierter Refresh-Token).</returns>
    public async Task<RefreshSessionResult> RefreshSessionAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        return await PostJsonAsync<RefreshSessionResult>(
            "api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = refreshToken },
            cancellationToken,
            allowReauth: false);
    }

    /// <summary>
    /// Widerruft den Refresh-Token serverseitig
    /// (<c>POST api/auth/logout</c>). Der Endpunkt ist idempotent und
    /// antwortet auch bei bereits ungültigem Token mit HTTP 200.
    /// </summary>
    /// <param name="refreshToken">Der zu widerrufende Refresh-Token.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    public async Task LogoutSessionAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        await PostJsonAsync(
            "api/auth/logout",
            new RefreshTokenRequest { RefreshToken = refreshToken },
            cancellationToken,
            allowReauth: false);
    }

    #endregion

    #region Health

    /// <summary>
    /// Prüft die Erreichbarkeit des Servers (<c>GET api/health</c>) und
    /// unterscheidet dabei eine Gate-Ablehnung (HTTP 401) von echter
    /// Nichterreichbarkeit.
    /// </summary>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Das Ergebnis der Erreichbarkeitsprüfung.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await SendWithPolicyAsync(
                "api/health",
                () => new HttpRequestMessage(HttpMethod.Get, "api/health"),
                allowReauth: true,
                cancellationToken);
            return response.IsSuccessStatusCode ? HealthCheckResult.Reachable : HealthCheckResult.Unreachable;
        }
        catch (ApiException ex)
        {
            return ex.StatusCode == HttpStatusCode.Unauthorized
                ? HealthCheckResult.GateRejected
                : HealthCheckResult.Unreachable;
        }
    }

    #endregion

    #region Sources

    /// <summary>Liefert alle freigegebenen Medienquellen (<c>GET api/Sources</c>).</summary>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Liste der Medienquellen (ggf. leer).</returns>
    public async Task<IReadOnlyList<MediaSource>> GetSourcesAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<MediaSource[]>("api/Sources", cancellationToken) ?? [];
    }

    /// <summary>Liefert eine einzelne Medienquelle (<c>GET api/Sources/{id}</c>).</summary>
    /// <param name="sourceId">ID der Medienquelle.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die angeforderte Medienquelle.</returns>
    public async Task<MediaSource> GetSourceAsync(long sourceId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceId);
        return await GetJsonAsync<MediaSource>($"api/Sources/{sourceId}", cancellationToken);
    }

    /// <summary>Liefert die Genres einer Medienquelle (<c>GET api/SourceGenres/{id}</c>).</summary>
    /// <param name="sourceId">ID der Medienquelle.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die Genres der Medienquelle.</returns>
    public async Task<SourceGenres> GetSourceGenresAsync(long sourceId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceId);
        return await GetJsonAsync<SourceGenres>($"api/SourceGenres/{sourceId}", cancellationToken);
    }

    #endregion

    #region Recent Entries

    /// <summary>Liefert die zuletzt hinzugefügten Inhalte (<c>GET api/items/recent</c>).</summary>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Liste der Neuzugänge (ggf. leer).</returns>
    public async Task<IReadOnlyList<RecentEntry>> GetRecentEntriesAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<RecentEntry[]>("api/items/recent", cancellationToken) ?? [];
    }

    #endregion

    #region Continue Watching

    /// <summary>Liefert die Weiterschauen-Liste (<c>GET api/continue-watching</c>).</summary>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Liste der Weiterschauen-Einträge (ggf. leer).</returns>
    public async Task<IReadOnlyList<ContinueWatchingEntry>> GetContinueWatchingAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<ContinueWatchingEntry[]>("api/continue-watching", cancellationToken) ?? [];
    }

    /// <summary>Meldet den Wiedergabefortschritt (<c>POST api/continue-watching/progress</c>).</summary>
    /// <param name="mediaType">Medientyp (z. B. <c>movie</c>, <c>episode</c>).</param>
    /// <param name="mediaId">ID des Mediums.</param>
    /// <param name="positionSeconds">Aktuelle Wiedergabeposition in Sekunden.</param>
    /// <param name="durationSeconds">Gesamtdauer des Mediums in Sekunden.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    public async Task ReportPlaybackProgressAsync(
        string mediaType,
        long mediaId,
        long positionSeconds,
        long durationSeconds,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mediaId);

        await PostJsonAsync(
            "api/continue-watching/progress",
            new PlaybackProgressReport
            {
                MediaType = mediaType,
                MediaId = mediaId,
                PositionSeconds = positionSeconds,
                DurationSeconds = durationSeconds
            },
            cancellationToken);
    }

    /// <summary>Blendet einen Eintrag der Weiterschauen-Liste aus (<c>POST api/continue-watching/hide</c>).</summary>
    /// <param name="mediaType">Medientyp des Eintrags.</param>
    /// <param name="mediaId">ID des Mediums.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Ergebnis der Mutation.</returns>
    public async Task<ContinueWatchingMutationResult> HideContinueWatchingEntryAsync(
        string mediaType,
        long mediaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mediaId);

        return await PostJsonAsync<ContinueWatchingMutationResult>(
            "api/continue-watching/hide",
            new { MediaType = mediaType, MediaId = mediaId },
            cancellationToken);
    }

    /// <summary>Überspringt einen Eintrag der Weiterschauen-Liste (<c>POST api/continue-watching/skip</c>).</summary>
    /// <param name="mediaType">Medientyp des Eintrags.</param>
    /// <param name="mediaId">ID des Mediums.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Ergebnis der Mutation.</returns>
    public async Task<ContinueWatchingMutationResult> SkipContinueWatchingEntryAsync(
        string mediaType,
        long mediaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mediaId);

        return await PostJsonAsync<ContinueWatchingMutationResult>(
            "api/continue-watching/skip",
            new { MediaType = mediaType, MediaId = mediaId },
            cancellationToken);
    }

    #endregion

    #region Favorites

    /// <summary>Liefert die Favoriten (<c>GET api/favorites</c>).</summary>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Liste der Favoriteneinträge (ggf. leer).</returns>
    public async Task<IReadOnlyList<FavoriteEntry>> GetFavoritesAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<FavoriteEntry[]>("api/favorites", cancellationToken) ?? [];
    }

    /// <summary>Schaltet die Favoriten-Markierung eines Mediums um (<c>POST api/favorites/toggle</c>).</summary>
    /// <param name="entry">Medieneintrag, dessen Favoriten-Status umgeschaltet wird.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Der neue Favoriten-Status (<c>true</c> = Favorit).</returns>
    public async Task<bool> ToggleFavoriteAsync(MediaEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return await PostJsonAsync<bool>("api/favorites/toggle", entry, cancellationToken);
    }

    /// <summary>Entfernt einen Favoriten (<c>POST api/favorites/remove</c>).</summary>
    /// <param name="favoriteId">ID des zu entfernenden Favoriteneintrags.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    public async Task RemoveFavoriteAsync(long favoriteId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(favoriteId);
        await PostJsonAsync(
            "api/favorites/remove",
            new { Id = favoriteId, UserId = "anonymous" },
            cancellationToken);
    }

    #endregion

    #region Media Entries

    /// <summary>
    /// Liefert paginierte Einträge einer Quelle, optional mit Textsuche und
    /// Genre-Filter (<c>GET api/items</c>).
    /// </summary>
    /// <param name="mediaSourceId">ID der Medienquelle.</param>
    /// <param name="page">Nullbasierte Seitennummer.</param>
    /// <param name="pageSize">Einträge pro Seite.</param>
    /// <param name="searchText">Optionaler Suchtext für die Titelsuche.</param>
    /// <param name="genreId">Optionaler Genre-Filter (ID des Genres).</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Liste der Einträge der angeforderten Seite (ggf. leer).</returns>
    public async Task<IReadOnlyList<MediaItemSummary>> GetSourceItemsAsync(
        long mediaSourceId,
        int page = 0,
        int pageSize = 30,
        string? searchText = null,
        long? genreId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(mediaSourceId);
        ArgumentOutOfRangeException.ThrowIfNegative(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        var url = $"api/items?mediaSourceId={mediaSourceId}&page={page}&size={pageSize}";
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            url += $"&search={Uri.EscapeDataString(searchText)}";
        }
        if (genreId is > 0)
        {
            url += $"&genreId={genreId}";
        }

        return await GetJsonAsync<MediaItemSummary[]>(url, cancellationToken) ?? [];
    }

    /// <summary>Liefert einen Film (<c>GET api/items/movie/{id}</c>).</summary>
    /// <param name="id">ID des Films.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Der angeforderte Film.</returns>
    public async Task<Movie> GetMovieAsync(long id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return await GetJsonAsync<Movie>($"api/items/movie/{id}", cancellationToken);
    }

    /// <summary>Liefert eine Filmkollektion (<c>GET api/items/moviecollection/{id}</c>).</summary>
    /// <param name="id">ID der Kollektion.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die angeforderte Filmkollektion.</returns>
    public async Task<MovieCollection> GetMovieCollectionAsync(long id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return await GetJsonAsync<MovieCollection>($"api/items/moviecollection/{id}", cancellationToken);
    }

    /// <summary>Liefert eine Serie inklusive Staffeln (<c>GET api/items/tvshow/{id}</c>).</summary>
    /// <param name="id">ID der Serie.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die angeforderte Serie.</returns>
    public async Task<TvShow> GetTvShowAsync(long id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return await GetJsonAsync<TvShow>($"api/items/tvshow/{id}", cancellationToken);
    }

    /// <summary>Liefert eine Episode (<c>GET api/items/tvshowepisode/{id}</c>).</summary>
    /// <param name="id">ID der Episode.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die angeforderte Episode.</returns>
    public async Task<TvShowEpisode> GetTvShowEpisodeAsync(long id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return await GetJsonAsync<TvShowEpisode>($"api/items/tvshowepisode/{id}", cancellationToken);
    }

    /// <summary>Liefert die verfügbaren Genre-Optionen (<c>GET api/items/genres</c>).</summary>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Liste der Genre-Optionen (ggf. leer).</returns>
    public async Task<IReadOnlyList<GenreOption>> GetGenreOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<GenreOption[]>("api/items/genres", cancellationToken) ?? [];
    }

    #endregion

    #region Pictures

    /// <summary>Lädt ein Medienbild (<c>GET api/pictures/{id}</c>).</summary>
    /// <param name="pictureId">ID des Bilds.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die Bilddaten als Byte-Array.</returns>
    public async Task<byte[]> GetPictureAsync(long pictureId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pictureId);
        using var response = await SendWithPolicyAsync(
            $"api/pictures/{pictureId}",
            () => new HttpRequestMessage(HttpMethod.Get, $"api/pictures/{pictureId}"),
            allowReauth: true,
            cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>Lädt ein Quellen-Icon (<c>GET api/sourceicons/{id}</c>).</summary>
    /// <param name="pictureId">ID des Bilds.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die Bilddaten als Byte-Array.</returns>
    public async Task<byte[]> GetSourceIconAsync(long pictureId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pictureId);
        using var response = await SendWithPolicyAsync(
            $"api/sourceicons/{pictureId}",
            () => new HttpRequestMessage(HttpMethod.Get, $"api/sourceicons/{pictureId}"),
            allowReauth: true,
            cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Lädt das servergenerierte Hero-Hintergrundbild der Startseite
    /// (<c>GET api/pictures/hero-background</c>). Der Server liefert ein
    /// generiertes JPEG bzw. einen serverseitigen Platzhalter.
    /// </summary>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die Bilddaten als Byte-Array.</returns>
    public async Task<byte[]> GetHeroBackgroundAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendWithPolicyAsync(
            "api/pictures/hero-background",
            () => new HttpRequestMessage(HttpMethod.Get, "api/pictures/hero-background"),
            allowReauth: true,
            cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Lädt das Hintergrundbild einer Episode (<c>GET
    /// api/episodes/{id}/background-image</c>). Der Server liefert das
    /// generierte Episoden-Standbild, fällt auf Banner/Fanart zurück
    /// oder sendet einen Platzhalter.
    /// </summary>
    /// <param name="episodeId">ID der Episode.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die Bilddaten als Byte-Array.</returns>
    public async Task<byte[]> GetEpisodeBackgroundImageAsync(long episodeId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(episodeId);
        using var response = await SendWithPolicyAsync(
            $"api/episodes/{episodeId}/background-image",
            () => new HttpRequestMessage(HttpMethod.Get, $"api/episodes/{episodeId}/background-image"),
            allowReauth: true,
            cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    #endregion

    #region Playlists

    /// <summary>
    /// Liefert die Playlists des angemeldeten Benutzers
    /// (<c>GET api/playlists</c>), optional auf Playlists mit einem
    /// bestimmten Genre eingeschränkt.
    /// </summary>
    /// <param name="genreId">Optionaler Genre-Filter (ID des Genres).</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Liste der eigenen Playlists (ggf. leer).</returns>
    public async Task<IReadOnlyList<Playlist>> GetPlaylistsAsync(
        long? genreId = null,
        CancellationToken cancellationToken = default)
    {
        var url = "api/playlists";
        if (genreId is > 0)
        {
            url += $"?genreId={genreId}";
        }

        return await GetJsonAsync<Playlist[]>(url, cancellationToken) ?? [];
    }

    /// <summary>
    /// Liefert alle als öffentlich markierten Playlists
    /// (<c>GET api/playlists/public</c>), optional auf Playlists mit
    /// einem bestimmten Genre eingeschränkt.
    /// </summary>
    /// <param name="genreId">Optionaler Genre-Filter (ID des Genres).</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Liste der öffentlichen Playlists (ggf. leer).</returns>
    public async Task<IReadOnlyList<Playlist>> GetPublicPlaylistsAsync(
        long? genreId = null,
        CancellationToken cancellationToken = default)
    {
        var url = "api/playlists/public";
        if (genreId is > 0)
        {
            url += $"?genreId={genreId}";
        }

        return await GetJsonAsync<Playlist[]>(url, cancellationToken) ?? [];
    }

    /// <summary>
    /// Liefert eine einzelne Playlist (<c>GET api/playlists/{id}</c>).
    /// </summary>
    /// <param name="playlistId">ID der Playlist.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die angeforderte Playlist bzw. <c>null</c>, wenn sie nicht existiert (HTTP 404).</returns>
    public async Task<Playlist?> GetPlaylistAsync(long playlistId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playlistId);
        try
        {
            return await GetJsonAsync<Playlist>($"api/playlists/{playlistId}", cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>
    /// Liefert eine sortierte, paginierte Seite der Einträge einer
    /// Playlist (<c>GET api/playlists/{id}/entries/paged</c>). Die
    /// Einträge kommen in der serverseitigen Reihenfolge — der Client
    /// sortiert nicht um.
    /// </summary>
    /// <param name="playlistId">ID der Playlist.</param>
    /// <param name="pageNumber">1-basierte Nummer der zu ladenden Seite.</param>
    /// <param name="pageSize">Maximale Einträge pro Seite.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die angeforderte Eintragsseite.</returns>
    public async Task<PlaylistEntriesPagedResult> GetPlaylistEntriesPagedAsync(
        long playlistId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playlistId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        return await GetJsonAsync<PlaylistEntriesPagedResult>(
            $"api/playlists/{playlistId}/entries/paged?pageNumber={pageNumber}&pageSize={pageSize}",
            cancellationToken);
    }

    /// <summary>
    /// Lädt das Cover-Bild einer Playlist (<c>GET api/playlists/{id}/cover</c>).
    /// </summary>
    /// <param name="playlistId">ID der Playlist.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die Bilddaten als Byte-Array bzw. <c>null</c>, wenn die Playlist kein Cover hat (HTTP 404).</returns>
    public async Task<byte[]?> GetPlaylistCoverAsync(long playlistId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(playlistId);
        try
        {
            using var response = await SendWithPolicyAsync(
                $"api/playlists/{playlistId}/cover",
                () => new HttpRequestMessage(HttpMethod.Get, $"api/playlists/{playlistId}/cover"),
                allowReauth: true,
                cancellationToken);
            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    #endregion

    #region Send pipeline

    private async Task<T> GetJsonAsync<T>(string endpoint, CancellationToken cancellationToken)
    {
        using var response = await SendWithPolicyAsync(
            endpoint,
            () => new HttpRequestMessage(HttpMethod.Get, endpoint),
            allowReauth: true,
            cancellationToken);
        return await DeserializeAsync<T>(endpoint, response, cancellationToken);
    }

    private async Task<T> PostJsonAsync<T>(string endpoint, object payload, CancellationToken cancellationToken, bool allowReauth = true)
    {
        using var response = await SendWithPolicyAsync(
            endpoint,
            () => CreateJsonPost(endpoint, payload),
            allowReauth,
            cancellationToken);
        return await DeserializeAsync<T>(endpoint, response, cancellationToken);
    }

    private async Task PostJsonAsync(string endpoint, object payload, CancellationToken cancellationToken, bool allowReauth = true)
    {
        using var response = await SendWithPolicyAsync(
            endpoint,
            () => CreateJsonPost(endpoint, payload),
            allowReauth,
            cancellationToken);
    }

    private static HttpRequestMessage CreateJsonPost(string endpoint, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json")
        };
        return request;
    }

    /// <summary>
    /// Sendet eine Anfrage mit der festen Wiederholungsstrategie: ein
    /// Initialversuch plus bis zu drei Wiederholungen (Warteintervalle 1 s, 2 s,
    /// 4 s) für wiederholbare Fehler; bei 401 einmalige Token-Erneuerung mit
    /// Neuversand außerhalb des Wiederholungsbudgets.
    /// </summary>
    /// <param name="endpoint">Relativer Endpunktpfad (für Fehlermeldungen und Logging).</param>
    /// <param name="requestFactory">Fabrik für die Anfrage; wird pro Versand neu aufgerufen.</param>
    /// <param name="allowReauth">Ob bei 401 eine einmalige Token-Erneuerung versucht wird.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    /// <returns>Die erfolgreiche Antwort.</returns>
    /// <exception cref="ApiException">Nicht wiederholbarer oder nach den Wiederholungen weiterhin bestehender Fehler.</exception>
    private async Task<HttpResponseMessage> SendWithPolicyAsync(
        string endpoint,
        Func<HttpRequestMessage> requestFactory,
        bool allowReauth,
        CancellationToken cancellationToken)
    {
        var reauthAttempted = false;
        var attempt = 0;

        while (true)
        {
            HttpResponseMessage response;
            try
            {
                using var request = requestFactory();
                response = await httpClient.SendAsync(request, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                if (attempt >= MaxRetries)
                {
                    throw new ApiException(endpoint, null, null,
                        $"Anfrage an {endpoint} nach {attempt + 1} Versuchen fehlgeschlagen: {ex.Message}", ex);
                }

                options.Logger?.LogWarning(ex, "Transportfehler bei {Endpoint}; Wiederholung {Attempt} nach {Delay}.", endpoint, attempt + 1, RetryDelays[attempt]);
                await DelayAsync(RetryDelays[attempt], cancellationToken);
                attempt++;
                continue;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (allowReauth && !reauthAttempted)
                {
                    reauthAttempted = true;
                    options.Logger?.LogWarning("401 von {Endpoint}; versuche einmalige Token-Erneuerung.", endpoint);
                    if (await TryRefreshTokenAsync(cancellationToken))
                    {
                        response.Dispose();
                        continue;
                    }
                }

                throw await CreateApiExceptionAsync(endpoint, response, cancellationToken);
            }

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            if (IsRetriable(response.StatusCode) && attempt < MaxRetries)
            {
                options.Logger?.LogWarning("Status {StatusCode} von {Endpoint}; Wiederholung {Attempt} nach {Delay}.", (int)response.StatusCode, endpoint, attempt + 1, RetryDelays[attempt]);
                response.Dispose();
                await DelayAsync(RetryDelays[attempt], cancellationToken);
                attempt++;
                continue;
            }

            throw await CreateApiExceptionAsync(endpoint, response, cancellationToken);
        }
    }

    private static bool IsRetriable(HttpStatusCode statusCode)
    {
        return (int)statusCode >= 500
            || statusCode == HttpStatusCode.RequestTimeout
            || statusCode == HttpStatusCode.TooManyRequests;
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        var refresher = options.TokenRefresher;
        if (refresher is null)
        {
            return false;
        }

        await tokenRefreshGate.WaitAsync(cancellationToken);
        try
        {
            var token = await refresher(cancellationToken);
            if (token is null || string.IsNullOrWhiteSpace(token.Token))
            {
                return false;
            }

            SetAccessToken(token);
            return true;
        }
        finally
        {
            tokenRefreshGate.Release();
        }
    }

    private async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        var delayAsync = options.DelayAsync ?? Task.Delay;
        await delayAsync(delay, cancellationToken);
    }

    private static async Task<ApiException> CreateApiExceptionAsync(string endpoint, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var statusCode = response.StatusCode;
        var reason = response.ReasonPhrase;
        response.Dispose();
        return new ApiException(endpoint, statusCode, body, $"Anfrage an {endpoint} fehlgeschlagen: {(int)statusCode} {reason}");
    }

    private static async Task<T> DeserializeAsync<T>(string endpoint, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<T>(content, JsonOptions)
            ?? throw new InvalidOperationException($"Antwort von {endpoint} konnte nicht deserialisiert werden.");
    }

    #endregion
}
