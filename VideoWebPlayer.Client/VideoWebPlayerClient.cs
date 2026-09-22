using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using VideoWebPlayer.Client.Models;
using VideoWebPlayer.Controllers.Models;
using VideoWebPlayer.Data;

namespace VideoWebPlayer.Client
{
    /// <summary>
    /// HTTP-Client für die Kommunikation mit der VideoWebPlayer-Server-API.
    /// Kapselt Authentifizierung, automatische Token-Erneuerung bei 401-Antworten
    /// sowie alle fachlichen Endpunkte (Quellen, Medieneinträge, Favoriten,
    /// "Weiter ansehen", Bilder).
    /// </summary>
    public class VideoWebPlayerClient
    {
        private readonly HttpClient httpClient;
        private readonly ConcurrentDictionary<string, ProgressSendState> progressStates = new();

        /// <summary>
        /// Erstellt einen neuen Client für die VideoWebPlayer-Server-API.
        /// </summary>
        /// <param name="httpClient">Der für Serveranfragen verwendete HTTP-Client.</param>
        /// <param name="logger">Logger für Diagnosemeldungen.</param>
        public VideoWebPlayerClient(HttpClient httpClient, ILogger<VideoWebPlayerClient> logger)
        {
            this.httpClient = httpClient;
            Logger = logger;
        }

        private sealed class ProgressSendState
        {
            public readonly object Gate = new();
            public bool IsSending { get; set; }
            public ProgressPayload? Buffered { get; set; }
        }

        private sealed class ProgressPayload
        {
            public required string MediaType { get; init; }
            public required long MediaId { get; init; }
            public required long PositionSeconds { get; init; }
            public required long DurationSeconds { get; init; }
        }

        /// <summary>
        /// Wird aufgerufen, wenn eine Anfrage mit 401 Unauthorized fehlschlägt.
        /// Abgeleitete Klassen können hier z. B. das Token erneuern.
        /// </summary>
        /// <returns>
        /// <c>true</c>, wenn ein neues Token verfügbar ist und die Anfrage wiederholt
        /// werden soll; andernfalls <c>false</c>.
        /// </returns>
        protected virtual Task<bool> HandleUnauthorized()
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Führt eine GET-Anfrage an den angegebenen Endpunkt aus und deserialisiert
        /// die JSON-Antwort. Bei 401 wird zunächst <see cref="HandleUnauthorized"/>
        /// aufgerufen und die Anfrage ggf. wiederholt.
        /// </summary>
        /// <typeparam name="T">Der erwartete Antworttyp.</typeparam>
        /// <param name="endPoint">Der abzufragende API-Endpunkt.</param>
        /// <returns>Die deserialisierte Antwort.</returns>
        /// <exception cref="HttpRequestException">Bei 401 ohne erfolgreiche Token-Erneuerung oder bei einem Fehlerstatuscode.</exception>
        protected virtual async Task<T> HttpGetAsync<T>(string endPoint)
        {
            async Task<HttpResponseMessage> DoRequestAsync() => await httpClient.GetAsync(endPoint);

            var response = await DoRequestAsync();

            // Prüfe auf 401 Unauthorized
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Logger?.LogWarning("Received 401 Unauthorized from {EndPoint}. Token might be expired.", endPoint);

                if (await HandleUnauthorized())
                {
                    response = await DoRequestAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        // Wenn nach Erneuerung weiterhin Unauthorized kommt, gib das weiter.
                        Logger?.LogWarning("Retry after token refresh still returned 401 for {EndPoint}.", endPoint);
                        throw new HttpRequestException($"Unauthorized: {endPoint}");
                    }
                }
                else
                {
                    // Kein neuer Token innerhalb der Wartezeit
                    throw new HttpRequestException($"Unauthorized: {endPoint}");
                }
            }

            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to GET from {endPoint}: {response.ReasonPhrase}");
            return System.Text.Json.JsonSerializer.Deserialize<T>(content, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        /// <summary>
        /// Führt eine POST-Anfrage an den angegebenen Endpunkt aus und deserialisiert
        /// die JSON-Antwort. Bei 401 wird zunächst <see cref="HandleUnauthorized"/>
        /// aufgerufen und die Anfrage ggf. wiederholt.
        /// </summary>
        /// <typeparam name="T">Der erwartete Antworttyp.</typeparam>
        /// <param name="endPoint">Der aufzurufende API-Endpunkt.</param>
        /// <param name="args">Der zu sendende HTTP-Inhalt.</param>
        /// <param name="skipReauthorize">
        /// <c>true</c>, um bei einer 401-Antwort keine erneute Autorisierung zu
        /// versuchen (z. B. innerhalb des Login-Aufrufs selbst).
        /// </param>
        /// <returns>Die deserialisierte Antwort.</returns>
        /// <exception cref="HttpRequestException">Bei 401 ohne erfolgreiche Token-Erneuerung oder bei einem Fehlerstatuscode.</exception>
        protected virtual async Task<T> HttpPostAsync<T>(string endPoint, HttpContent args, bool skipReauthorize = false)
        {
            async Task<HttpResponseMessage> DoRequestAsync() => await httpClient.PostAsync(endPoint, args);

            var response = await DoRequestAsync();
            // Prüfe auf 401 Unauthorized
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Logger?.LogWarning("Received 401 Unauthorized from {EndPoint}. Token might be expired.", endPoint);
                if (skipReauthorize)
                {
                    // We're in the login call itself; do not attempt to re-authorize.
                    Logger?.LogWarning("Skipping reauthorization for login request to {EndPoint}.", endPoint);
                    throw new HttpRequestException($"Unauthorized: {endPoint}");
                }

                if (await HandleUnauthorized())
                {
                    response = await DoRequestAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        // Wenn nach Erneuerung weiterhin Unauthorized kommt, gib das weiter.
                        Logger?.LogWarning("Retry after token refresh still returned 401 for {EndPoint}.", endPoint);
                        throw new HttpRequestException($"Unauthorized: {endPoint}");
                    }
                }
                else
                {
                    // Kein neuer Token innerhalb der Wartezeit
                    throw new HttpRequestException($"Unauthorized: {endPoint}");
                }
            }

            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    string.IsNullOrWhiteSpace(content)
                        ? $"Failed to POST from {endPoint}: {response.ReasonPhrase}"
                        : content);
            return System.Text.Json.JsonSerializer.Deserialize<T>(content, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Deserialization returned null.");
        }

        /// <summary>
        /// Führt eine POST-Anfrage an den angegebenen Endpunkt aus, ohne eine
        /// Antwort zu deserialisieren. Bei 401 wird zunächst
        /// <see cref="HandleUnauthorized"/> aufgerufen und die Anfrage ggf. wiederholt.
        /// </summary>
        /// <param name="endPoint">Der aufzurufende API-Endpunkt.</param>
        /// <param name="args">Der zu sendende HTTP-Inhalt.</param>
        /// <param name="skipReauthorize">
        /// <c>true</c>, um bei einer 401-Antwort keine erneute Autorisierung zu versuchen.
        /// </param>
        /// <exception cref="HttpRequestException">Bei 401 ohne erfolgreiche Token-Erneuerung oder bei einem Fehlerstatuscode.</exception>
        protected virtual async Task HttpPostAsync(string endPoint, HttpContent args, bool skipReauthorize = false)
        {
            async Task<HttpResponseMessage> DoRequestAsync() => await httpClient.PostAsync(endPoint, args);

            var response = await DoRequestAsync();
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Logger?.LogWarning("Received 401 Unauthorized from {EndPoint}. Token might be expired.", endPoint);
                if (skipReauthorize)
                    throw new HttpRequestException($"Unauthorized: {endPoint}");

                if (await HandleUnauthorized())
                {
                    response = await DoRequestAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        throw new HttpRequestException($"Unauthorized: {endPoint}");
                }
                else
                {
                    throw new HttpRequestException($"Unauthorized: {endPoint}");
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    string.IsNullOrWhiteSpace(content)
                        ? $"Failed to POST from {endPoint}: {response.ReasonPhrase}"
                        : content);
            }
        }

        #region Authentication
        /// <summary>
        /// Authentifiziert den Benutzer am Login-Endpunkt und speichert das
        /// zurückgegebene Autorisierungstoken im HTTP-Client.
        /// </summary>
        /// <param name="email">E-Mail-Adresse des Benutzers.</param>
        /// <param name="password">Passwort des Benutzers.</param>
        /// <returns>Das vom Server zurückgegebene Autorisierungstoken.</returns>
        public async Task<AuthorizationToken?> AuthenticateAsync(string email, string password)
        {
            var request = new AuthenticationRequest { Email = email, Password = password };
            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var token = await HttpPostAsync<AuthorizationToken>("api/auth/login", content, skipReauthorize: true);
            SetAuthorizationToken(token);
            return token;
        }

        /// <summary>
        /// Setzt das Bearer-Token im Authorization-Header des HTTP-Clients.
        /// </summary>
        /// <param name="token">Das zu setzende Autorisierungstoken.</param>
        public virtual void SetAuthorizationToken(AuthorizationToken token)
        {
            if (token is not null)
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.token);
            }
        }

        /// <summary>
        /// Aktuell gesetztes Bearer-Token oder <c>null</c>, wenn keines gesetzt ist.
        /// </summary>
        public string? AuthorizationToken
        {
            get
            {
                return httpClient.DefaultRequestHeaders.Authorization?.Parameter;
            }
        }

        /// <summary>
        /// Stellt sicher, dass für den aktuellen Benutzer ein Autorisierungstoken
        /// verfügbar ist. Die Standardimplementierung tut nichts; abgeleitete
        /// Klassen können hier eine Impersonation durchführen.
        /// </summary>
        /// <param name="user">Der aktuelle Benutzer, für den ein Token sichergestellt werden soll.</param>
        /// <param name="cancellationToken">Token zum Abbrechen des Vorgangs.</param>
        public virtual Task EnsureAuthorizationTokenAsync(ClaimsPrincipal? user, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gibt an, ob sich der Client noch in der Initialisierungsphase befindet.
        /// </summary>
        public bool Initializing { get; set; }
        /// <summary>
        /// Logger für Diagnosemeldungen.
        /// </summary>
        protected ILogger<VideoWebPlayerClient> Logger { get; }
        #endregion

        #region HealthCheck
        /// <summary>
        /// Prüft, ob der Server erreichbar und betriebsbereit ist.
        /// </summary>
        /// <returns>
        /// <c>true</c>, wenn der Server einen Erfolgsstatuscode zurückgibt;
        /// andernfalls <c>false</c>.
        /// </returns>
        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                var response = await httpClient.GetAsync("api/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Sources
        /// <summary>
        /// Fragt alle verfügbaren Medienquellen vom Server ab.
        /// </summary>
        /// <returns>Liste der Medienquellen; bei einem Fehler eine leere Liste.</returns>
        public async Task<IEnumerable<DtoMediaSource>> RequestSourcesAsync()
        {
            try
            {
                return await HttpGetAsync<DtoMediaSource[]>("api/Sources");
            }
            catch
            {
                return new DtoMediaSource[0];
            }
        }
        /// <summary>
        /// Fragt eine einzelne Medienquelle vom Server ab.
        /// </summary>
        /// <param name="sourceId">ID der Medienquelle.</param>
        /// <returns>Die Medienquelle oder <c>null</c> bei einem Fehler.</returns>
        public async Task<DtoMediaSource?> RequestSourceAsync(long sourceId)
        {
            try
            {
                return await HttpGetAsync<DtoMediaSource>($"api/Sources/{sourceId}");
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Fragt die Genres einer Medienquelle vom Server ab.
        /// </summary>
        /// <param name="sourceId">ID der Medienquelle.</param>
        /// <returns>Die Genres der Quelle oder <c>null</c> bei einem Fehler.</returns>
        public async Task<SourceGenresDto?> RequestSourceGenresAsync(long sourceId)
        {
            try
            {
                return await HttpGetAsync<SourceGenresDto>($"api/SourceGenres/{sourceId}");
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region Recent Entries
        /// <summary>
        /// Fragt die Liste der kürzlich hinzugefügten Medieneinträge ab.
        /// </summary>
        /// <returns>Liste der kürzlich hinzugefügten Einträge.</returns>
        public async Task<IEnumerable<DtoRecentEntry>> RequestRecentEntriesAsync()
        {
            return await HttpGetAsync<DtoRecentEntry[]>("api/items/recent");
        }
        #endregion

        #region Continue Watching
        /// <summary>
        /// Fragt die "Weiter ansehen"-Liste des aktuellen Benutzers ab.
        /// </summary>
        /// <returns>Liste der "Weiter ansehen"-Einträge.</returns>
        public async Task<IEnumerable<ContinueWatchingDto>> RequestContinueWatchingAsync()
        {
            return await HttpGetAsync<ContinueWatchingDto[]>("api/continue-watching");
        }

        /// <summary>
        /// Blendet einen Eintrag der "Weiter ansehen"-Liste aus.
        /// </summary>
        /// <param name="mediaType">Medientyp des Eintrags (z. B. "movie" oder "episode").</param>
        /// <param name="mediaId">ID des Medieneintrags.</param>
        /// <returns>Ergebnis der Mutation.</returns>
        public async Task<ContinueWatchingMutationResult> HideContinueWatchingAsync(string mediaType, long mediaId)
        {
            var json = JsonSerializer.Serialize(new { MediaType = mediaType, MediaId = mediaId });
            return await HttpPostAsync<ContinueWatchingMutationResult>(
                "api/continue-watching/hide",
                new StringContent(json, System.Text.Encoding.UTF8, new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")));
        }

        /// <summary>
        /// Überspringt einen Eintrag der "Weiter ansehen"-Liste.
        /// </summary>
        /// <param name="mediaType">Medientyp des Eintrags (z. B. "movie" oder "episode").</param>
        /// <param name="mediaId">ID des Medieneintrags.</param>
        /// <returns>Ergebnis der Mutation.</returns>
        public async Task<ContinueWatchingMutationResult> SkipContinueWatchingAsync(string mediaType, long mediaId)
        {
            var json = JsonSerializer.Serialize(new { MediaType = mediaType, MediaId = mediaId });
            return await HttpPostAsync<ContinueWatchingMutationResult>(
                "api/continue-watching/skip",
                new StringContent(json, System.Text.Encoding.UTF8, new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")));
        }
        #endregion

        #region Favorites
        /// <summary>
        /// Fragt die Favoritenliste des aktuellen Benutzers ab.
        /// </summary>
        /// <returns>Liste der Favoriten-Einträge.</returns>
        public async Task<IEnumerable<DtoFavoriteEntry>> RequestFavoritesAsync()
        {
            return await HttpGetAsync<DtoFavoriteEntry[]>("api/favorites");
        }
        /// <summary>
        /// Schaltet den Favoritenstatus eines Medieneintrags um.
        /// </summary>
        /// <param name="entry">Der Medieneintrag, dessen Favoritenstatus umgeschaltet wird.</param>
        /// <returns>Der neue Favoritenstatus (<c>true</c> = Favorit).</returns>
        public async Task<bool> ToggleFavorite(DtoMediaEntry entry)
        {
            var json = JsonSerializer.Serialize(entry);
            return await HttpPostAsync<bool>("api/favorites/toggle", new StringContent(json, new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")));
        }

        /// <summary>
        /// Entfernt einen Favoriten-Eintrag.
        /// </summary>
        /// <param name="favoriteId">ID des zu entfernenden Favoriten-Eintrags.</param>
        public async Task RemoveFavoriteAsync(long favoriteId)
        {
            var json = JsonSerializer.Serialize(new { Id = favoriteId, UserId = "anonymous" });
            await HttpPostAsync("api/favorites/remove", new StringContent(json, System.Text.Encoding.UTF8, new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")));
        }
        #endregion

        #region Media Entries
        /// <summary>
        /// Fragt die Medieneinträge einer Medienquelle seitenweise ab, optional
        /// gefiltert nach Suchtext und Genre.
        /// </summary>
        /// <param name="mediaSourceId">ID der Medienquelle.</param>
        /// <param name="Page">Seitennummer (0-basiert).</param>
        /// <param name="PageSize">Anzahl der Einträge pro Seite.</param>
        /// <param name="searchText">Optionaler Suchtext zur Filterung.</param>
        /// <param name="genreId">Optionale Genre-ID zur Filterung (0 = kein Filter).</param>
        /// <returns>Liste der Medieneinträge der angeforderten Seite.</returns>
        public async Task<List<MediaEntryDto>> RequestSourceItems(long mediaSourceId, int Page = 0, int PageSize = 30, string searchText = "", long genreId = 0)
        {
            var url = $"/api/items?mediaSourceId={mediaSourceId}&page={Page}&size={PageSize}";
            if (!string.IsNullOrWhiteSpace(searchText))
                url += $"&search={Uri.EscapeDataString(searchText)}";
            if (genreId > 0)
                url += $"&genreId={genreId}";
            return await HttpGetAsync<List<MediaEntryDto>>(url);
        }

        /// <summary>
        /// Fragt eine Filmsammlung vom Server ab.
        /// </summary>
        /// <param name="id">ID der Filmsammlung.</param>
        /// <returns>Die Filmsammlung als Medieneintrag.</returns>
        public async Task<DtoMediaEntry> RequestMovieCollectionAsync(long id)
        {
            return await HttpGetAsync<DtoMovieCollection>($"api/items/moviecollection/{id}");
        }

        /// <summary>
        /// Fragt einen Film vom Server ab.
        /// </summary>
        /// <param name="id">ID des Films.</param>
        /// <returns>Der Film oder <c>null</c> bei einem Fehler.</returns>
        public async Task<DtoMovie?> RequestMovieAsync(long id)
        {
            try
            {
                return await HttpGetAsync<DtoMovie>($"api/items/movie/{id}");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Fragt eine Fernsehserie vom Server ab.
        /// </summary>
        /// <param name="id">ID der Serie.</param>
        /// <returns>Die Serie als Medieneintrag.</returns>
        public async Task<DtoMediaEntry> RequestTVShowAsync(long id)
        {
            return await HttpGetAsync<DtoTVShow>($"api/items/tvshow/{id}");
        }

        /// <summary>
        /// Fragt eine Episode einer Fernsehserie vom Server ab.
        /// </summary>
        /// <param name="id">ID der Episode.</param>
        /// <returns>Die Episode oder <c>null</c> bei einem Fehler.</returns>
        public async Task<DtoTVShowEpisode?> RequestTVShowEpisodeAsync(long id)
        {
            try
            {
                return await HttpGetAsync<DtoTVShowEpisode>($"api/items/tvshowepisode/{id}");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Fragt die verfügbaren Genre-Auswahloptionen vom Server ab.
        /// </summary>
        /// <returns>Liste der Genre-Optionen; bei einem Fehler eine leere Liste.</returns>
        public async Task<List<DtoGenreOption>> RequestGenreOptionsAsync()
        {
            try
            {
                return await HttpGetAsync<List<DtoGenreOption>>("api/items/genres");
            }
            catch
            {
                return [];
            }
        }

        /// <summary>
        /// Speichert geänderte Metadaten eines Medieneintrags auf dem Server.
        /// </summary>
        /// <param name="request">Die Metadaten-Aktualisierungsanfrage.</param>
        public async Task SaveMetadataAsync(MediaMetadataUpdateRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            _ = await HttpPostAsync<bool>(
                "api/items/metadata",
                new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        }

        #endregion

        #region Continue Watching
        /// <summary>
        /// Ruft die "Weiter ansehen"-Liste des aktuellen Benutzers ab.
        /// </summary>
        /// <returns>Liste der "Weiter ansehen"-Einträge.</returns>
        public async Task<List<ContinueWatchingDto>> GetContinueWatchingAsync()
        {
            return await HttpGetAsync<List<ContinueWatchingDto>>("api/continue-watching");
        }

        /// <summary>
        /// Meldet den Wiedergabefortschritt des aktuellen Benutzers an den Server.
        /// Coalescing: Für ein MediaItem läuft nur ein Request gleichzeitig.
        /// Währenddessen wird genau eine letzte Meldung gepuffert (überschreibend).
        /// </summary>
        /// <param name="mediaType">Medientyp des Eintrags (z. B. "movie" oder "episode").</param>
        /// <param name="mediaId">ID des Medieneintrags.</param>
        /// <param name="positionSeconds">Aktuelle Wiedergabeposition in Sekunden.</param>
        /// <param name="durationSeconds">Gesamtdauer des Mediums in Sekunden.</param>
        public async Task ReportPlaybackProgressAsync(string mediaType, long mediaId, long positionSeconds, long durationSeconds)
        {
            var key = $"{mediaType}:{mediaId}";
            var payload = new ProgressPayload
            {
                MediaType = mediaType,
                MediaId = mediaId,
                PositionSeconds = positionSeconds,
                DurationSeconds = durationSeconds
            };

            var state = progressStates.GetOrAdd(key, _ => new ProgressSendState());

            bool shouldSendNow;
            lock (state.Gate)
            {
                if (!state.IsSending)
                {
                    state.IsSending = true;
                    shouldSendNow = true;
                }
                else
                {
                    state.Buffered = payload;
                    shouldSendNow = false;
                }
            }

            if (!shouldSendNow)
                return;

            var current = payload;
            try
            {
                while (true)
                {
                    await SendPlaybackProgressCoreAsync(current);

                    ProgressPayload? next;
                    lock (state.Gate)
                    {
                        next = state.Buffered;
                        state.Buffered = null;

                        if (next is null)
                        {
                            state.IsSending = false;
                            break;
                        }
                    }

                    current = next;
                }
            }
            finally
            {
                lock (state.Gate)
                {
                    if (!state.IsSending && state.Buffered is null)
                    {
                        progressStates.TryRemove(key, out _);
                    }
                }
            }
        }

        private async Task SendPlaybackProgressCoreAsync(ProgressPayload payload)
        {
            var body = new
            {
                MediaType = payload.MediaType,
                MediaId = payload.MediaId,
                PositionSeconds = payload.PositionSeconds,
                DurationSeconds = payload.DurationSeconds
            };

            var json = System.Text.Json.JsonSerializer.Serialize(body);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var endPoint = "api/continue-watching/progress";
            async Task<HttpResponseMessage> DoRequestAsync() => await httpClient.PostAsync(endPoint, content);
            var response = await DoRequestAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Logger?.LogWarning("Received 401 Unauthorized from continue-watching/progress. Token might be expired.");
                if (await HandleUnauthorized())
                {
                    response = await DoRequestAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        // Wenn nach Erneuerung weiterhin Unauthorized kommt, gib das weiter.
                        Logger?.LogWarning("Retry after token refresh still returned 401 for {EndPoint}.", endPoint);
                        throw new HttpRequestException($"Unauthorized: {endPoint}");
                    }
                }
                else
                {
                    // Kein neuer Token innerhalb der Wartezeit
                    throw new HttpRequestException($"Unauthorized: {endPoint}");
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to report progress: {response.ReasonPhrase}");
            }
        }
        /// <summary>
        /// Fetches the picture data for a given picture ID.
        /// </summary>
        /// <param name="pictureId">The ID of the picture to fetch.</param>
        /// <returns>A byte array containing the picture data, or an empty array if the picture could not be fetched.</returns>
        public async Task<byte[]> GetPictureAsync(long pictureId)
        {
            try
            {
                var response = await httpClient.GetAsync($"api/pictures/{pictureId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error fetching picture with ID {PictureId}", pictureId);
            }
            return Array.Empty<byte>();
        }
        /// <summary>
        /// Fetches the source picture data for a given picture ID.
        /// </summary>
        /// <param name="pictureId">The ID of the source picture to fetch.</param>
        /// <returns>A byte array containing the source picture data, or an empty array if the picture could not be fetched.</returns>
        public async Task<byte[]> GetSourcePictureAsync(long pictureId)
        {
            try
            {
                var iconUrl = $"api/sourceicons/{pictureId}";
                var response = await httpClient.GetAsync(iconUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error fetching source picture with ID {PictureId}", pictureId);
            }
            return Array.Empty<byte>();
        }
        /// <summary>
        /// Deletes a media source via the admin API.
        /// </summary>
        /// <param name="sourceId">The source identifier.</param>
        public async Task DeleteSourceAsync(long sourceId)
        {
            var endPoint = $"api/admin/sources/{sourceId}";
            var response = await httpClient.DeleteAsync(endPoint);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to delete source {sourceId}: {content}");
            }
        }

        #endregion
    }
}
