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

    #region Authentication

    /// <summary>
    /// Meldet den Benutzer am Server an (<c>POST api/auth/login</c>) und setzt
    /// das erhaltene Token für Folgeanfragen.
    /// </summary>
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

    #endregion

    #region Health

    /// <summary>Prüft die Erreichbarkeit des Servers (<c>GET api/health</c>).</summary>
    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await SendWithPolicyAsync(
                "api/health",
                () => new HttpRequestMessage(HttpMethod.Get, "api/health"),
                allowReauth: true,
                cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (ApiException)
        {
            return false;
        }
    }

    #endregion

    #region Sources

    /// <summary>Liefert alle freigegebenen Medienquellen (<c>GET api/Sources</c>).</summary>
    public async Task<IReadOnlyList<MediaSource>> GetSourcesAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<MediaSource[]>("api/Sources", cancellationToken) ?? [];
    }

    /// <summary>Liefert eine einzelne Medienquelle (<c>GET api/Sources/{id}</c>).</summary>
    public async Task<MediaSource> GetSourceAsync(long sourceId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceId);
        return await GetJsonAsync<MediaSource>($"api/Sources/{sourceId}", cancellationToken);
    }

    /// <summary>Liefert die Genres einer Medienquelle (<c>GET api/SourceGenres/{id}</c>).</summary>
    public async Task<SourceGenres> GetSourceGenresAsync(long sourceId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sourceId);
        return await GetJsonAsync<SourceGenres>($"api/SourceGenres/{sourceId}", cancellationToken);
    }

    #endregion

    #region Recent Entries

    /// <summary>Liefert die zuletzt hinzugefügten Inhalte (<c>GET api/items/recent</c>).</summary>
    public async Task<IReadOnlyList<RecentEntry>> GetRecentEntriesAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<RecentEntry[]>("api/items/recent", cancellationToken) ?? [];
    }

    #endregion

    #region Continue Watching

    /// <summary>Liefert die Weiterschauen-Liste (<c>GET api/continue-watching</c>).</summary>
    public async Task<IReadOnlyList<ContinueWatchingEntry>> GetContinueWatchingAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<ContinueWatchingEntry[]>("api/continue-watching", cancellationToken) ?? [];
    }

    /// <summary>Meldet den Wiedergabefortschritt (<c>POST api/continue-watching/progress</c>).</summary>
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
    public async Task<IReadOnlyList<FavoriteEntry>> GetFavoritesAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<FavoriteEntry[]>("api/favorites", cancellationToken) ?? [];
    }

    /// <summary>Schaltet die Favoriten-Markierung eines Mediums um (<c>POST api/favorites/toggle</c>).</summary>
    public async Task<bool> ToggleFavoriteAsync(MediaEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return await PostJsonAsync<bool>("api/favorites/toggle", entry, cancellationToken);
    }

    /// <summary>Entfernt einen Favoriten (<c>POST api/favorites/remove</c>).</summary>
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
    public async Task<Movie> GetMovieAsync(long id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return await GetJsonAsync<Movie>($"api/items/movie/{id}", cancellationToken);
    }

    /// <summary>Liefert eine Filmkollektion (<c>GET api/items/moviecollection/{id}</c>).</summary>
    public async Task<MovieCollection> GetMovieCollectionAsync(long id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return await GetJsonAsync<MovieCollection>($"api/items/moviecollection/{id}", cancellationToken);
    }

    /// <summary>Liefert eine Serie inklusive Staffeln (<c>GET api/items/tvshow/{id}</c>).</summary>
    public async Task<TvShow> GetTvShowAsync(long id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return await GetJsonAsync<TvShow>($"api/items/tvshow/{id}", cancellationToken);
    }

    /// <summary>Liefert eine Episode (<c>GET api/items/tvshowepisode/{id}</c>).</summary>
    public async Task<TvShowEpisode> GetTvShowEpisodeAsync(long id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return await GetJsonAsync<TvShowEpisode>($"api/items/tvshowepisode/{id}", cancellationToken);
    }

    /// <summary>Liefert die verfügbaren Genre-Optionen (<c>GET api/items/genres</c>).</summary>
    public async Task<IReadOnlyList<GenreOption>> GetGenreOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await GetJsonAsync<GenreOption[]>("api/items/genres", cancellationToken) ?? [];
    }

    #endregion

    #region Pictures

    /// <summary>Lädt ein Medienbild (<c>GET api/pictures/{id}</c>).</summary>
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
