using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VideoPlayer.E2E.Tests.Support;

/// <summary>
/// Hermetischer Mini-Nachbau der App-seitig genutzten Server-Endpunkte
/// (<c>api/health</c>, <c>api/pairing/exchange</c>, <c>api/auth/login</c>
/// sowie die nach der Anmeldung abgefragten Listen). Bindet auf
/// <c>127.0.0.1</c> mit systemvergebenem Port; jede Verbindung wird
/// einzeln beantwortet (<c>Connection: close</c>). Unbekannte Routen
/// liefern 404 — niemals 5xx, damit die Client-Wiederholungsstrategie
/// nicht anspringt.
/// </summary>
public sealed class MockVideoServer : IDisposable
{
    private readonly TcpListener listener;
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task acceptLoop;
    private readonly string plainAuthToken;
    private readonly string adminAuthToken;
    private long nextPlaylistId = 900_000;
    private long nextEntryId = 910_000;

    /// <summary>Erstellt und startet den Mock-Server.</summary>
    /// <param name="pairingCode">Der gültige Einmal-Pairing-Code.</param>
    /// <param name="deviceToken">Das auszustellende Geräte-Token (Gate-Schlüssel).</param>
    /// <param name="username">Gültige Anmelde-E-Mail.</param>
    /// <param name="password">Gültiges Anmelde-Passwort.</param>
    /// <param name="bootstrapTicket">Das gültige Bootstrap-Ticket (QR-Code); <c>null</c> = Standardwert.</param>
    /// <param name="bootstrapShortCode">Der gültige Kurzcode-Alias des Tickets; <c>null</c> = Standardwert.</param>
    public MockVideoServer(
        string pairingCode,
        string deviceToken,
        string username,
        string password,
        string? bootstrapTicket = null,
        string? bootstrapShortCode = null)
    {
        PairingCode = pairingCode;
        DeviceToken = deviceToken;
        Username = username;
        Password = password;
        BootstrapTicket = bootstrapTicket ?? $"e2e-ticket-{Guid.NewGuid():N}";
        BootstrapShortCode = bootstrapShortCode ?? "E2ETICK1";
        plainAuthToken = $"e2e-auth-{Guid.NewGuid():N}";
        adminAuthToken = BuildAdminToken();
        RefreshToken = $"e2e-refresh-{Guid.NewGuid():N}";

        listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        BaseAddress = $"http://127.0.0.1:{Port}/";
        acceptLoop = Task.Run(AcceptLoopAsync);
    }

    /// <summary>Systemvergebener Port des Servers.</summary>
    public int Port { get; }

    /// <summary>Basisadresse in der kanonischen Form <c>http://127.0.0.1:port/</c>.</summary>
    public string BaseAddress { get; }

    /// <summary>Der gültige Pairing-Code.</summary>
    public string PairingCode { get; }

    /// <summary>Das Geräte-Token, das <c>exchange</c> ausstellt und das Gate akzeptiert.</summary>
    public string DeviceToken { get; }

    /// <summary>Gültige Anmelde-E-Mail.</summary>
    public string Username { get; }

    /// <summary>Gültiges Anmelde-Passwort.</summary>
    public string Password { get; }

    /// <summary>Das ausgestellte Anmelde-Token — bei <see cref="LoginIsAdmin"/> ein JWT-artiger Token mit <c>IsAdmin</c>-Claim (die App dekodiert die Payload ohne Signaturprüfung).</summary>
    public string AuthToken => LoginIsAdmin ? adminAuthToken : plainAuthToken;

    /// <summary>
    /// Opt-in Administrator-Token: <c>true</c> stellt ein Token mit
    /// <c>IsAdmin = "True"</c>-Claim aus — die App blendet den
    /// Öffentlich-Schalter der Playlist ein; <c>false</c> (Standard)
    /// der bisherige opake Token ohne Claims.
    /// </summary>
    public bool LoginIsAdmin { get; set; }

    /// <summary>Das gültige Bootstrap-Ticket (langer, opaker Wert aus dem QR-Code).</summary>
    public string BootstrapTicket { get; }

    /// <summary>Der Kurzcode-Alias des Bootstrap-Tickets (8 Zeichen, Server-seitig aufgelöst).</summary>
    public string BootstrapShortCode { get; }

    /// <summary>Der aktuell gültige Refresh-Token (rotiert bei jeder Erneuerung).</summary>
    public string RefreshToken { get; private set; }

    /// <summary>Anzahl eingelöster Pairing-Requests (Diagnose).</summary>
    public int PairingExchangeCount { get; private set; }

    /// <summary>Anzahl eingelöster Bootstrap-Requests (Diagnose).</summary>
    public int BootstrapCount { get; private set; }

    /// <summary>Anzahl erfolgreicher Sitzungserneuerungen (Diagnose).</summary>
    public int RefreshCount { get; private set; }

    /// <summary>Anzahl eingegangener Logout-Aufrufe (Diagnose).</summary>
    public int LogoutCount { get; private set; }

    /// <summary>Anzahl erfolgreicher Logins (Diagnose).</summary>
    public int LoginCount { get; private set; }

    /// <summary>Body der letzten Login-Anfrage (Diagnose bei Fehlschlägen).</summary>
    public string? LastLoginRequestBody { get; private set; }

    /// <summary>Gate-Header der letzten Login-Anfrage (Diagnose bei Fehlschlägen).</summary>
    public string? LastLoginGateKey { get; private set; }

    /// <summary>Alle empfangenen Login-Bodies in Reihenfolge (Diagnose).</summary>
    public List<string> LoginRequestBodies { get; } = [];

    /// <summary>
    /// Opt-in Wiedergabefluss: Id des angebotenen Films; <c>null</c> =
    /// Standard (leere Listen, unbekannte Item-/Stream-Routen liefern
    /// 404). Belegt liefert „Neu im Programm" den Movie-Eintrag
    /// <see cref="PlaybackMovieTitle"/>, die Detail-Route die Movie-JSON,
    /// „continue-watching" optional einen Fortsetzungseintrag
    /// (<see cref="PlaybackResumePositionSeconds"/>) und der Stream-
    /// Endpunkt <see cref="PlaybackVideoPath"/> (oder 404 bei
    /// <see cref="PlaybackStreamFails"/>).
    /// </summary>
    public long? PlaybackMovieId { get; set; }

    /// <summary>Anzeigetitel des Opt-in-Wiedergabefluss-Films.</summary>
    public string PlaybackMovieTitle { get; set; } = "E2E-Testfilm";

    /// <summary>Pfad der MP4-Datei, die der Stream-Endpunkt ausliefert.</summary>
    public string? PlaybackVideoPath { get; set; }

    /// <summary>Weiterschauen-Position in Sekunden; <c>null</c> = kein Fortsetzungseintrag.</summary>
    public long? PlaybackResumePositionSeconds { get; set; }

    /// <summary><c>true</c> = der Stream-Endpunkt antwortet mit 404 (Wiedergabefehler-Pfad).</summary>
    public bool PlaybackStreamFails { get; set; }

    /// <summary>
    /// Opt-in Serienfluss: Id der Serie, zu der <see cref="PlaybackEpisodeId"/>
    /// und <see cref="PlaybackNextEpisodeId"/> gehören (belegt liefert
    /// <c>GET api/items/tvshow/{id}</c> die Serie mit beiden Episoden in
    /// einer Staffel und <c>api/continue-watching</c> einen
    /// Episoden-Eintrag mit eingebettetem Seriengraph).
    /// </summary>
    public long? PlaybackShowId { get; set; }

    /// <summary>Anzeigetitel der Opt-in-Serie.</summary>
    public string PlaybackShowTitle { get; set; } = "E2E-Testserie";

    /// <summary>Id der Episoden-Weiterschauen-Eintrags (Opt-in Serienfluss).</summary>
    public long? PlaybackEpisodeId { get; set; }

    /// <summary>Anzeigetitel der Ausgangs-Episode.</summary>
    public string PlaybackEpisodeTitle { get; set; } = "E2E-Episode 1";

    /// <summary>Id der Folge-Episode (nächste Episode der Serie).</summary>
    public long? PlaybackNextEpisodeId { get; set; }

    /// <summary>Anzeigetitel der Folge-Episode.</summary>
    public string PlaybackNextEpisodeTitle { get; set; } = "E2E-Episode 2";

    /// <summary>Weiterschauen-Position der Episode in Sekunden; <c>null</c> = kein Episoden-Fortsetzungseintrag.</summary>
    public long? PlaybackEpisodeResumePositionSeconds { get; set; }

    /// <summary>
    /// Opt-in Playlistfluss: die Playlist-Testdaten. Einträge mit
    /// <see cref="MockPlaylist.IsOwner"/> erscheinen in
    /// <c>GET /api/playlists</c>, solche mit <see cref="MockPlaylist.IsPublic"/>
    /// in <c>GET /api/playlists/public</c>; Detail-, Eintrags- und
    /// Cover-Endpunkte liefern die konfigurierten Daten der jeweiligen
    /// Playlist.
    /// </summary>
    public List<MockPlaylist> Playlists { get; } = [];

    /// <summary>
    /// Opt-in Genre-Optionen (<c>GET api/items/genres</c>) — die
    /// wählbaren Genres des Playlist-Genre-Editors.
    /// </summary>
    public List<MockGenreOption> GenreOptions { get; } = [];

    /// <summary>
    /// Bekannte Medientitel des „Zu Playlist hinzufügen"-Flusses
    /// (Schlüssel <c>"{mediaType}:{mediaId}"</c>) — Titel des neu
    /// angelegten Playlist-Eintrags; unbekannte Medien heißen
    /// <c>"Medium {id}"</c>.
    /// </summary>
    public List<MockKnownMedia> KnownMedia { get; } = [];

    /// <summary><c>true</c> = alle Playlist-Endpunkte antworten mit 404 (Fehlerpfad der Playlists-Anzeige).</summary>
    public bool PlaylistsUnavailable { get; set; }

    /// <summary>Namen der angelegten Playlists in Reihenfolge (POST /api/playlists, Diagnose).</summary>
    public List<string> CreatedPlaylistNames { get; } = [];

    /// <summary>Anzahl eingegangener Playlist-Aktualisierungen (PUT /api/playlists/{id}, Diagnose).</summary>
    public int UpdatedPlaylistCount { get; private set; }

    /// <summary>Anzahl eingegangener Playlist-Löschungen (DELETE /api/playlists/{id}, Diagnose).</summary>
    public int DeletedPlaylistCount { get; private set; }

    /// <summary>Hinzugefügte Einträge im Format <c>"{playlistId}:{mediaType}:{mediaId}"</c> (Diagnose).</summary>
    public List<string> AddedPlaylistEntries { get; } = [];

    /// <summary>Bestätigungsflagge je Entfernen-Aufruf (DELETE …/entries/{type}/{id}) in Reihenfolge — Nachweis der 409-Wiederholung.</summary>
    public List<bool> RemoveEntryConfirmFlags { get; } = [];

    /// <summary>Angeforderte Sortiermodus-Wechsel im Format <c>"{newSortMode}|{confirmed}"</c> (PATCH …/sort-mode, Diagnose).</summary>
    public List<string> SortModeRequests { get; } = [];

    /// <summary>Gesetzte Genre-Ids je PUT …/genres-Aufruf in Reihenfolge (Diagnose).</summary>
    public List<long[]> GenresSetRequests { get; } = [];

    /// <summary>Anzahl eingegangener Genre-Rücksetzungen (POST …/genres/reset, Diagnose).</summary>
    public int GenresResetCount { get; private set; }

    /// <summary>Gesetzte Öffentlich-Werte je PUT …/public-Aufruf in Reihenfolge (Diagnose).</summary>
    public List<bool> PublicFlagRequests { get; } = [];

    /// <summary>Eintrags-Ids der „An den Anfang"-Aufrufe in Reihenfolge (Diagnose).</summary>
    public List<long> MoveToBeginningRequests { get; } = [];

    /// <summary>Eintrags-Ids der „Zwischenposition"-Aufrufe in Reihenfolge (POST …/move-between, Diagnose).</summary>
    public List<long> MoveBetweenRequests { get; } = [];

    /// <summary>Anzahl eingegangener direkter SortOrder-Setzungen (PUT …/order, Diagnose).</summary>
    public int ReorderEntryCount { get; private set; }

    /// <summary>
    /// Opt-in nicht abspielbarer Weiterschauen-Eintrag: Id eines
    /// Serieneintrags (<c>mediaType: "show"</c>) — öffnet die
    /// Detailansicht statt der Wiedergabe.
    /// </summary>
    public long? ContinueWatchingShowId { get; set; }

    /// <summary>Anzeigetitel des nicht abspielbaren Weiterschauen-Serieneintrags.</summary>
    public string ContinueWatchingShowTitle { get; set; } = "E2E-Serie";

    /// <summary>
    /// Opt-in Staffelstruktur der Detailserie: befüllt liefert
    /// <c>GET api/items/tvshow/{id}</c> für <see cref="ContinueWatchingShowId"/>
    /// die Serie inklusive dieser Staffeln und Episoden (Serienansicht
    /// mit Staffel-Picker); leer = bisheriges Verhalten ohne Staffeln.
    /// </summary>
    public List<MockShowSeason> ShowSeasons { get; } = [];

    /// <summary>
    /// Opt-in Playlist-Bezug der Weiterschauen-Einträge (Film und
    /// Episode): Id der Playlist, aus deren Kontext der Eintrag
    /// entstand; <c>null</c> = kein Playlist-Bezug (Serienpfad).
    /// </summary>
    public long? ContinueWatchingPlaylistId { get; set; }

    /// <summary>Name der Playlist des Weiterschauen-Bezugs; <c>null</c> = kein Bezug.</summary>
    public string? ContinueWatchingPlaylistName { get; set; }

    /// <summary>Id des Playlist-Eintrags des Weiterschauen-Bezugs; <c>null</c> = kein Bezug.</summary>
    public long? ContinueWatchingPlaylistEntryId { get; set; }

    /// <summary>
    /// Opt-in Quellen-Browsing: die freigegebenen Quellen des
    /// <c>GET /api/Sources</c>-Endpunkts — belegt statt der
    /// Standard-Leerliste; jede Quelle erzeugt einen eigenen
    /// Menüeintrag in der App (<c>sourceoverview</c>-Route).
    /// </summary>
    public List<MockSource> Sources { get; } = [];

    /// <summary>
    /// Opt-in Genre-Listen je Quelle (<c>GET api/SourceGenres/{id}</c>,
    /// Schlüssel = Quellen-Id); nicht konfigurierte Quellen liefern
    /// eine leere Genre-Liste (befundetes Serververhalten „keine
    /// Genres für die Quelle").
    /// </summary>
    public Dictionary<long, List<MockSourceGenre>> SourceGenres { get; } = [];

    /// <summary>
    /// Opt-in Einträge des Quellen-Browsing
    /// (<c>GET api/items?mediaSourceId=…</c>): gefiltert nach
    /// Quellen-Id, <c>genreId</c> (<see cref="MockSourceItem.GenreIds"/>)
    /// und <c>search</c> (Titel-Teilstring), paginiert über
    /// <c>page</c>/<c>size</c>.
    /// </summary>
    public List<MockSourceItem> SourceItems { get; } = [];

    /// <summary>
    /// Rohe Request-Pfade inklusive Query der Quellen-Eintragsaufrufe
    /// (<c>GET api/items?mediaSourceId=…</c>) in Reihenfolge — Nachweis
    /// der gesendeten <c>genreId</c>/<c>search</c>-Parameter (Diagnose).
    /// </summary>
    public List<string> SourceItemRequests { get; } = [];

    /// <summary>Alle empfangenen Anfragepfade in Reihenfolge (Diagnose: Detail- vs. Wiedergabeaufrufe).</summary>
    public List<string> RequestedPaths { get; } = [];

    /// <summary>Anzahl eingegangener <c>POST /api/continue-watching/hide</c>-Aufrufe (Diagnose).</summary>
    public int HideContinueWatchingCount { get; private set; }

    /// <summary>Anzahl eingegangener <c>POST /api/continue-watching/skip</c>-Aufrufe (Diagnose).</summary>
    public int SkipContinueWatchingCount { get; private set; }

    /// <summary>
    /// Per hide/skip entfernte Weiterschauen-Einträge im Format
    /// <c>"{mediaType}:{mediaId}"</c> — <c>GET /api/continue-watching</c>
    /// liefert diese Einträge danach nicht mehr (Serverstand).
    /// </summary>
    public List<string> RemovedContinueWatchingKeys { get; } = [];

    /// <summary>
    /// Prüft, ob ein Anfragepfad empfangen wurde (Thread-sicherer Leser;
    /// gespeicherte Einträge im Format <c>"{Methode} {Pfad}"</c>,
    /// Vergleich ohne Query-String).
    /// </summary>
    /// <param name="path">Der gesuchte Pfad ohne Methode (z. B. <c>/api/items/movie/7</c>).</param>
    /// <returns><c>true</c>, wenn der Pfad mindestens einmal angefragt wurde.</returns>
    public bool WasPathRequested(string path)
    {
        lock (RequestedPaths)
        {
            return RequestedPaths.Any(entry =>
            {
                var space = entry.IndexOf(' ');
                var requestPath = space >= 0 ? entry[(space + 1)..] : entry;
                return requestPath.Split('?')[0].TrimEnd('/')
                    .Equals(path, StringComparison.OrdinalIgnoreCase);
            });
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        cancellation.Cancel();
        listener.Stop();
        try { acceptLoop.Wait(TimeSpan.FromSeconds(5)); } catch { /* best effort */ }
        cancellation.Dispose();
    }

    private async Task AcceptLoopAsync()
    {
        while (!cancellation.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync(cancellation.Token);
            }
            catch (OperationCanceledException) { break; }
            catch (SocketException) { break; }
            catch (ObjectDisposedException) { break; }

            _ = Task.Run(() => HandleClientAsync(client));
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                var request = await ReadRequestAsync(client.GetStream(), cancellation.Token);
                if (request is null)
                {
                    return;
                }

                var response = Route(request.Value);
                await WriteResponseAsync(client.GetStream(), response, cancellation.Token);
            }
            catch (Exception)
            {
                // Verbindungsabbruch durch den Client o. ä. — der Mock
                // darf keinen Testlauf über eine Exception beeinflussen.
            }
        }
    }

    /// <summary>
    /// Eine Antwort des Mock-Servers — Tupel-Notation der bisherigen
    /// Routen wird implizit konvertiert; zusätzliche Header nur für
    /// Endpunkte, die sie brauchen (Stream: Accept-Ranges/Content-Range).
    /// </summary>
    /// <param name="Status">HTTP-Statuscode der Antwort.</param>
    /// <param name="ContentType">MIME-Typ des Antwortkörpers.</param>
    /// <param name="Body">Der Antwortkörper (UTF-8-JSON oder Videodaten).</param>
    /// <param name="Headers">Zusätzliche Antwort-Header (optional).</param>
    /// <returns>Die Mock-Antwort.</returns>
    private sealed record MockResponse(
        int Status,
        string ContentType,
        byte[] Body,
        IReadOnlyDictionary<string, string>? Headers = null)
    {
        /// <summary>Wandelt das bisherige Antwort-Tupel um (ohne Zusatz-Header).</summary>
        /// <param name="tuple">Das Tupel (Status, ContentType, Body) der bisherigen Routen.</param>
        /// <returns>Die konvertierte Mock-Antwort ohne Zusatz-Header.</returns>
        public static implicit operator MockResponse((int Status, string ContentType, byte[] Body) tuple) =>
            new(tuple.Status, tuple.ContentType, tuple.Body);
    }

    private MockResponse Route((string Method, string Path, Dictionary<string, string> Headers, string Body) request)
    {
        var path = request.Path.Split('?')[0].TrimEnd('/');
        var method = request.Method;

        lock (RequestedPaths)
        {
            RequestedPaths.Add($"{method} {path}");
        }

        if (method == "GET" && path.Equals("/api/health", StringComparison.OrdinalIgnoreCase))
        {
            return (200, "text/plain", Encoding.UTF8.GetBytes("OK"));
        }

        if (method == "POST" && path.Equals("/api/pairing/exchange", StringComparison.OrdinalIgnoreCase))
        {
            return HandlePairingExchange(request.Body);
        }

        if (method == "POST" && path.Equals("/api/pairing/bootstrap", StringComparison.OrdinalIgnoreCase))
        {
            return HandleBootstrap(request.Body);
        }

        if (method == "POST" && path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            return HandleLogin(request);
        }

        if (method == "POST" && path.Equals("/api/auth/refresh", StringComparison.OrdinalIgnoreCase))
        {
            return HandleRefresh(request);
        }

        if (method == "POST" && path.Equals("/api/auth/logout", StringComparison.OrdinalIgnoreCase))
        {
            return HandleLogout(request);
        }

        if (method == "POST" && path.Equals("/api/continue-watching/progress", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsGateValid(request))
            {
                return GateRejected();
            }

            if (!IsBearerValid(request))
            {
                return (401, "text/plain", Encoding.UTF8.GetBytes("unauthorized"));
            }

            // Fortschritts-Senke: die Meldung wird angenommen, sonst nichts.
            return (204, "text/plain", Array.Empty<byte>());
        }

        // Kontextmenü-Mutationen der Weiterschauen-Liste: hide/skip
        // nehmen den Eintrag aus dem Serverstand (folgende Listenantworten
        // enthalten ihn nicht mehr).
        if (method == "POST" && path.Equals("/api/continue-watching/hide", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsGateValid(request))
            {
                return GateRejected();
            }

            if (!IsBearerValid(request))
            {
                return (401, "text/plain", Encoding.UTF8.GetBytes("unauthorized"));
            }

            HideContinueWatchingCount++;
            RecordRemoval(request.Body);
            return (200, "application/json", JsonBody(new { status = "hidden", message = "ok" }));
        }

        if (method == "POST" && path.Equals("/api/continue-watching/skip", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsGateValid(request))
            {
                return GateRejected();
            }

            if (!IsBearerValid(request))
            {
                return (401, "text/plain", Encoding.UTF8.GetBytes("unauthorized"));
            }

            SkipContinueWatchingCount++;
            RecordRemoval(request.Body);
            return (200, "application/json", JsonBody(new { status = "skipped", message = "ok" }));
        }

        // Opt-in Wiedergabefluss: Movie-Details und Stream-Endpunkt.
        if (method == "GET" && path.StartsWith("/api/items/movie/", StringComparison.OrdinalIgnoreCase))
        {
            if (path.EndsWith("/stream", StringComparison.OrdinalIgnoreCase))
            {
                // Der native Player sendet keine App-Header — die Stream-
                // URL authentifiziert sich über den access_token-Parameter
                // (Konvention des PlaybackSourceService/echten Servers).
                var token = QueryValue(request.Path, "access_token");
                if (!string.Equals(token, AuthToken, StringComparison.Ordinal))
                {
                    return (401, "text/plain", Encoding.UTF8.GetBytes("unauthorized"));
                }
            }
            else
            {
                if (!IsGateValid(request))
                {
                    return GateRejected();
                }

                if (!IsBearerValid(request))
                {
                    return (401, "text/plain", Encoding.UTF8.GetBytes("unauthorized"));
                }
            }

            return HandleMovieItem(path, request);
        }

        // Opt-in Serienfluss: Seriendetails und Episoden-Details/-Stream.
        if (method == "GET" && path.StartsWith("/api/items/tvshow/", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsGateValid(request))
            {
                return GateRejected();
            }

            if (!IsBearerValid(request))
            {
                return (401, "text/plain", Encoding.UTF8.GetBytes("unauthorized"));
            }

            return HandleTvShowItem(path);
        }

        if (method == "GET" && path.StartsWith("/api/items/tvshowepisode/", StringComparison.OrdinalIgnoreCase))
        {
            if (path.EndsWith("/stream", StringComparison.OrdinalIgnoreCase))
            {
                var token = QueryValue(request.Path, "access_token");
                if (!string.Equals(token, AuthToken, StringComparison.Ordinal))
                {
                    return (401, "text/plain", Encoding.UTF8.GetBytes("unauthorized"));
                }
            }
            else
            {
                if (!IsGateValid(request))
                {
                    return GateRejected();
                }

                if (!IsBearerValid(request))
                {
                    return (401, "text/plain", Encoding.UTF8.GetBytes("unauthorized"));
                }
            }

            return HandleEpisodeItem(path, request);
        }

        // Opt-in Genre-Optionen des Playlist-Genre-Editors.
        if (method == "GET" && path.Equals("/api/items/genres", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsGateValid(request))
            {
                return GateRejected();
            }

            if (!IsBearerValid(request))
            {
                return (401, "text/plain", Encoding.UTF8.GetBytes("unauthorized"));
            }

            return (200, "application/json", JsonBody(
                GenreOptions.Select(g => new { id = g.Id, name = g.Name })));
        }

        // Opt-in Quellen-Browsing: Genres einer Quelle — nicht
        // konfigurierte Quellen liefern eine leere Genre-Liste.
        if (method == "GET" && path.StartsWith("/api/SourceGenres/", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsGateValid(request))
            {
                return GateRejected();
            }

            if (!IsBearerValid(request))
            {
                return (401, "text/plain", Encoding.UTF8.GetBytes("unauthorized"));
            }

            return HandleSourceGenres(path);
        }

        // Opt-in Quellen-Browsing: paginierte Eintragsliste einer Quelle.
        if (method == "GET" && path.Equals("/api/items", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(QueryValue(request.Path, "mediaSourceId")))
        {
            if (!IsGateValid(request))
            {
                return GateRejected();
            }

            if (!IsBearerValid(request))
            {
                return (401, "text/plain", Encoding.UTF8.GetBytes("unauthorized"));
            }

            return HandleSourceItems(request.Path);
        }

        // Opt-in Playlistfluss: eigene + öffentliche Playlists, Detail,
        // Einträge (unpaginiert/paginiert), Cover, Wiedergabe-Endpunkte
        // sowie die Verwaltungs-Endpunkte (Anlage/Bearbeitung/Löschung,
        // Eintrags-Mutationen, Sortiermodus, Genres, Öffentlich-Flag) —
        // authentifiziert wie die übrigen Listen-Endpunkte.
        if (path.StartsWith("/api/playlists", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsGateValid(request))
            {
                return GateRejected();
            }

            if (!IsBearerValid(request))
            {
                return (401, "text/plain", Encoding.UTF8.GetBytes("unauthorized"));
            }

            return HandlePlaylistRequest(path, request.Path, method, request.Body);
        }

        // Authentifizierte Listen-Endpunkte: Gate + Bearer prüfen,
        // leere Liste liefern (ausreichend für Token-Validierung und Sync).
        // Ablehnung ohne Bearer → text/plain wie die Credential-Ablehnung
        // des echten Servers; Gate-Ablehnung → problem+json (Unterscheidung
        // der Gate-Probe, siehe DeviceTokenService.IsGateRejectedAsync).
        if (method == "GET" && path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            && IsListEndpoint(path))
        {
            if (!IsGateValid(request))
            {
                return GateRejected();
            }

            if (!IsBearerValid(request))
            {
                return (401, "text/plain", Encoding.UTF8.GetBytes("unauthorized"));
            }

            // Opt-in Wiedergabefluss: konfigurierte Listenantworten statt
            // der Standard-Leerliste.
            if (ListOverride(path) is { } overrideBody)
            {
                return (200, "application/json", overrideBody);
            }

            return (200, "application/json", JsonBody(Array.Empty<object>()));
        }

        return (404, "application/json", JsonBody(new { error = "not found" }));
    }

    /// <summary>
    /// Opt-in-Listenantworten des Wiedergabeflusses: „Neu im Programm"
    /// liefert den konfigurierten Movie-Eintrag, „continue-watching" den
    /// Fortsetzungseintrag (nur bei gesetzter <see cref="PlaybackResumePositionSeconds"/>).
    /// </summary>
    /// <param name="path">Der angefragte Request-Pfad (ohne Query).</param>
    /// <returns>Der überschreibende Antwortkörper bzw. <c>null</c> ohne Überschreibung.</returns>
    private byte[]? ListOverride(string path)
    {
        if (path.Equals("/api/Sources", StringComparison.OrdinalIgnoreCase)
            && Sources.Count > 0)
        {
            return JsonBody(Sources.Select(s => new { id = s.Id, name = s.Name }));
        }

        if (path.Equals("/api/items/recent", StringComparison.OrdinalIgnoreCase)
            && PlaybackMovieId is not null)
        {
            return JsonBody(new[]
            {
                new
                {
                    id = 1L,
                    mediaSourceId = 1L,
                    publishedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    // RecentEntryKind.Movie (numerisch serialisiert).
                    type = 0,
                    entry = new PlaybackMediaEntryJson
                    {
                        Id = PlaybackMovieId!.Value,
                        Name = PlaybackMovieTitle,
                        MediaSourceId = 1
                    }
                }
            });
        }

        if (path.Equals("/api/continue-watching", StringComparison.OrdinalIgnoreCase))
        {
            var entries = new List<object>();
            if (PlaybackResumePositionSeconds is > 0 && PlaybackMovieId is not null
                && !IsRemoved("movie", PlaybackMovieId.Value))
            {
                entries.Add(new
                {
                    mediaType = "movie",
                    entry = new PlaybackMediaEntryJson
                    {
                        Id = PlaybackMovieId!.Value,
                        Name = PlaybackMovieTitle,
                        MediaSourceId = 1
                    },
                    positionSeconds = PlaybackResumePositionSeconds.Value,
                    durationSeconds = 20L,
                    title = PlaybackMovieTitle,
                    playlistId = ContinueWatchingPlaylistId,
                    playlistName = ContinueWatchingPlaylistName,
                    playlistEntryId = ContinueWatchingPlaylistEntryId
                });
            }

            if (PlaybackEpisodeId is { } episodeId && !IsRemoved("episode", episodeId))
            {
                entries.Add(new
                {
                    mediaType = "episode",
                    entry = new PlaybackMediaEntryJson
                    {
                        Type = "episode",
                        Id = episodeId,
                        Name = PlaybackEpisodeTitle,
                        MediaSourceId = 1,
                        Number = 1,
                        Season = new PlaybackMediaEntryJson
                        {
                            Type = "season",
                            Id = 9001,
                            Name = "Staffel 1",
                            MediaSourceId = 1,
                            Number = 1,
                            Show = new PlaybackMediaEntryJson
                            {
                                Type = "show",
                                Id = PlaybackShowId ?? 0,
                                Name = PlaybackShowTitle,
                                MediaSourceId = 1
                            }
                        }
                    },
                    positionSeconds = PlaybackEpisodeResumePositionSeconds ?? 0L,
                    durationSeconds = 20L,
                    title = PlaybackEpisodeTitle,
                    playlistId = ContinueWatchingPlaylistId,
                    playlistName = ContinueWatchingPlaylistName,
                    playlistEntryId = ContinueWatchingPlaylistEntryId
                });
            }

            if (ContinueWatchingShowId is { } showId && !IsRemoved("show", showId))
            {
                entries.Add(new
                {
                    mediaType = "show",
                    entry = new PlaybackMediaEntryJson
                    {
                        Type = "show",
                        Id = showId,
                        Name = ContinueWatchingShowTitle,
                        MediaSourceId = 1
                    },
                    positionSeconds = 60L,
                    durationSeconds = 1200L,
                    title = ContinueWatchingShowTitle
                });
            }

            return entries.Count > 0 ? JsonBody(entries) : null;
        }

        return null;
    }

    /// <summary>
    /// Antwortet die Playlist-Endpunkte der App: eigene/öffentliche
    /// Listen (<c>/api/playlists</c>, <c>/api/playlists/public</c>),
    /// Einzel-Playlist (<c>/api/playlists/{id}</c>), Einträge
    /// (<c>/entries</c> und <c>/entries/paged</c>, letzteres in
    /// Server-Reihenfolge und 1-basiert), Cover (<c>/cover</c>),
    /// Wiedergabe (<c>/play…</c>) sowie die Verwaltungs-Endpunkte
    /// (POST/PUT/DELETE der Playlist, Eintrags-Mutationen mit
    /// Weiterschauen-Konflikt, Sortiermodus mit Verlust-Konflikt,
    /// Genres, Öffentlich-Flag). Die Server-Reihenfolge der Einträge
    /// ist ihr Listenindex — zugleich ihre <c>sortOrder</c>.
    /// <see cref="PlaylistsUnavailable"/> simuliert einen ausgefallenen
    /// Playlist-Bereich (404 auf allen Playlist-Routen).
    /// </summary>
    /// <param name="path">Der angefragte Pfad ohne Query-String.</param>
    /// <param name="rawPath">Der angefragte Pfad inkl. Query-String.</param>
    /// <param name="method">Die HTTP-Methode der Anfrage.</param>
    /// <param name="body">Der Anfragekörper (JSON bei Mutationen).</param>
    /// <returns>Die Mock-Antwort des Playlist-Endpunkts.</returns>
    private MockResponse HandlePlaylistRequest(string path, string rawPath, string method, string body)
    {
        if (PlaylistsUnavailable)
        {
            return (404, "application/json", JsonBody(new { error = "not found" }));
        }

        if (path.Equals("/api/playlists", StringComparison.OrdinalIgnoreCase))
        {
            // POST /api/playlists — Anlage einer eigenen Playlist.
            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                var (name, description, sortMode) = ReadPlaylistBody(body);
                if (string.IsNullOrWhiteSpace(name))
                {
                    return (400, "application/json", JsonBody(new { error = "name required" }));
                }

                var created = new MockPlaylist
                {
                    Id = nextPlaylistId++,
                    Name = name,
                    Description = description,
                    SortMode = string.IsNullOrWhiteSpace(sortMode) ? "Manual" : sortMode,
                    IsOwner = true
                };
                Playlists.Add(created);
                CreatedPlaylistNames.Add(name);
                return (200, "application/json", JsonBody(PlaylistJson(created)));
            }

            return (200, "application/json", JsonBody(
                Playlists.Where(p => p.IsOwner).Select(PlaylistJson)));
        }

        if (path.Equals("/api/playlists/public", StringComparison.OrdinalIgnoreCase))
        {
            return (200, "application/json", JsonBody(
                Playlists.Where(p => p.IsPublic).Select(PlaylistJson)));
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 3
            || !long.TryParse(segments[2], out var playlistId)
            || Playlists.FirstOrDefault(p => p.Id == playlistId) is not { } playlist)
        {
            return (404, "application/json", JsonBody(new { error = "not found" }));
        }

        if (segments.Length == 3)
        {
            if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
            {
                if (!playlist.IsOwner)
                {
                    return (403, "application/json", JsonBody(new { error = "forbidden" }));
                }

                var (name, description, _) = ReadPlaylistBody(body);
                if (string.IsNullOrWhiteSpace(name))
                {
                    return (400, "application/json", JsonBody(new { error = "name required" }));
                }

                playlist.Name = name;
                playlist.Description = description;
                UpdatedPlaylistCount++;
                return (200, "application/json", JsonBody(PlaylistJson(playlist)));
            }

            if (method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                if (!playlist.IsOwner)
                {
                    return (403, "application/json", JsonBody(new { error = "forbidden" }));
                }

                Playlists.Remove(playlist);
                DeletedPlaylistCount++;
                return (204, "text/plain", Array.Empty<byte>());
            }

            return (200, "application/json", JsonBody(PlaylistJson(playlist)));
        }

        if (segments.Length >= 4
            && segments[3].Equals("play", StringComparison.OrdinalIgnoreCase))
        {
            return HandlePlaylistPlay(segments, playlist, rawPath);
        }

        if (segments.Length == 4
            && segments[3].Equals("cover", StringComparison.OrdinalIgnoreCase))
        {
            return playlist.CoverPng is { Length: > 0 } png
                ? (MockResponse)new MockResponse(200, "image/png", png)
                : (404, "application/json", JsonBody(new { error = "no cover" }));
        }

        // PATCH /api/playlists/{id}/sort-mode — Wechsel mit
        // Verlust-Bestätigung, wenn die manuelle Reihenfolge verworfen wird.
        if (segments.Length == 4
            && segments[3].Equals("sort-mode", StringComparison.OrdinalIgnoreCase)
            && method.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
        {
            if (!playlist.IsOwner)
            {
                return (403, "application/json", JsonBody(new { error = "forbidden" }));
            }

            var newSortMode = ReadStringProperty(ParseBody(body), "newSortMode");
            var confirmed = ReadBoolProperty(ParseBody(body), "confirmLossOfManualOrder");
            SortModeRequests.Add($"{newSortMode}|{(confirmed ? "confirmed" : "unconfirmed")}");
            if (string.IsNullOrWhiteSpace(newSortMode))
            {
                return (400, "application/json", JsonBody(new { error = "sort mode required" }));
            }

            if (playlist.SortMode.Equals("Manual", StringComparison.OrdinalIgnoreCase)
                && !newSortMode.Equals("Manual", StringComparison.OrdinalIgnoreCase)
                && playlist.Entries.Count > 0
                && !confirmed)
            {
                return Conflict(lossOfData: true);
            }

            playlist.SortMode = newSortMode;
            return (200, "application/json", JsonBody(PlaylistJson(playlist)));
        }

        if (segments.Length >= 4
            && segments[3].Equals("genres", StringComparison.OrdinalIgnoreCase))
        {
            if (!playlist.IsOwner)
            {
                return (403, "application/json", JsonBody(new { error = "forbidden" }));
            }

            // POST …/genres/reset — zurück auf die berechneten Genres.
            if (segments.Length == 5
                && segments[4].Equals("reset", StringComparison.OrdinalIgnoreCase)
                && method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                playlist.GenresManuallyOverridden = false;
                GenresResetCount++;
                return (200, "application/json", JsonBody(PlaylistJson(playlist)));
            }

            // PUT …/genres — Anzeige-Genres setzen (markiert manuell).
            if (segments.Length == 4
                && method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
            {
                var ids = ReadLongArrayProperty(ParseBody(body), "genreIds");
                GenresSetRequests.Add(ids);
                playlist.GenreIds.Clear();
                playlist.GenreIds.AddRange(ids);
                playlist.Genres = ids
                    .Select(id => GenreOptions.FirstOrDefault(g => g.Id == id)?.Name ?? $"Genre {id}")
                    .ToArray();
                playlist.GenresManuallyOverridden = true;
                return (200, "application/json", JsonBody(PlaylistJson(playlist)));
            }
        }

        // PUT /api/playlists/{id}/public — nur Administrator:innen
        // (sonst 403 wie der echte Server).
        if (segments.Length == 4
            && segments[3].Equals("public", StringComparison.OrdinalIgnoreCase)
            && method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
        {
            if (!playlist.IsOwner || !LoginIsAdmin)
            {
                return (403, "application/json", JsonBody(new { error = "forbidden" }));
            }

            var isPublic = ReadBoolProperty(ParseBody(body), "isPublic");
            PublicFlagRequests.Add(isPublic);
            playlist.IsPublic = isPublic;
            return (200, "application/json", JsonBody(PlaylistJson(playlist)));
        }

        if (segments.Length >= 4
            && segments[3].Equals("entries", StringComparison.OrdinalIgnoreCase))
        {
            return HandlePlaylistEntries(segments, playlist, rawPath, method, body);
        }

        return (404, "application/json", JsonBody(new { error = "not found" }));
    }

    /// <summary>
    /// Antwortet die Eintrags-Endpunkte einer Playlist: GET-Listen
    /// (unpaginiert/paginiert/max-sort-order), POST-Anlage,
    /// DELETE-Entfernen (mit Weiterschauen-Konflikt) sowie die
    /// Verschiebe-Endpunkte (move-to-beginning/move-between/order).
    /// </summary>
    /// <param name="segments">Pfadsegmente des Requests.</param>
    /// <param name="playlist">Die aufgelöste Playlist.</param>
    /// <param name="rawPath">Der angefragte Pfad inkl. Query-String.</param>
    /// <param name="method">Die HTTP-Methode der Anfrage.</param>
    /// <param name="body">Der Anfragekörper (JSON bei Mutationen).</param>
    /// <returns>Die Mock-Antwort des Eintrags-Endpunkts.</returns>
    private MockResponse HandlePlaylistEntries(
        string[] segments, MockPlaylist playlist, string rawPath, string method, string body)
    {
        // POST /api/playlists/{id}/entries — Medium hinzufügen.
        if (segments.Length == 4 && method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            if (!playlist.IsOwner)
            {
                return (403, "application/json", JsonBody(new { error = "forbidden" }));
            }

            var mediaType = ReadStringProperty(ParseBody(body), "mediaType");
            var mediaId = ReadLongProperty(ParseBody(body), "mediaId");
            if (playlist.Entries.Any(e => e.MediaId == mediaId
                && e.MediaType.Equals(mediaType, StringComparison.OrdinalIgnoreCase)))
            {
                return (200, "application/json", JsonBody(new
                {
                    addedCount = 0,
                    skippedCount = 1,
                    message = "Das Medium ist bereits in der Playlist."
                }));
            }

            playlist.Entries.Add(new MockPlaylistEntry
            {
                Id = nextEntryId++,
                MediaType = mediaType,
                MediaId = mediaId,
                Title = ResolveMediaTitle(mediaType, mediaId),
                IsAccessible = true
            });
            AddedPlaylistEntries.Add($"{playlist.Id}:{mediaType}:{mediaId}");
            return (200, "application/json", JsonBody(new
            {
                addedCount = 1,
                skippedCount = 0,
                message = (string?)null
            }));
        }

        // DELETE /api/playlists/{id}/entries/{mediaType}/{mediaId} —
        // Entfernen; Einträge mit Weiterschauen-Bezug liefern ohne
        // Bestätigungsflagge die 409-Konfliktantwort.
        if (segments.Length == 6 && method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
        {
            if (!playlist.IsOwner)
            {
                return (403, "application/json", JsonBody(new { error = "forbidden" }));
            }

            var removeTarget = playlist.Entries.FirstOrDefault(e =>
                e.MediaType.Equals(segments[4], StringComparison.OrdinalIgnoreCase)
                && long.TryParse(segments[5], out var mid) && e.MediaId == mid);
            if (removeTarget is null)
            {
                return (404, "application/json", JsonBody(new { error = "not found" }));
            }

            var confirmed = string.Equals(
                QueryValue(rawPath, "confirmContinueWatchingRemoval"), "true", StringComparison.OrdinalIgnoreCase);
            RemoveEntryConfirmFlags.Add(confirmed);
            if (removeTarget.RequiresContinueWatchingRemovalConfirmation && !confirmed)
            {
                return Conflict(continueWatching: true);
            }

            playlist.Entries.Remove(removeTarget);
            return (204, "text/plain", Array.Empty<byte>());
        }

        // Aktionen eines Eintrags: …/entries/{entryId}/{aktion}.
        if (segments.Length == 6
            && long.TryParse(segments[4], out var entryId)
            && playlist.Entries.FirstOrDefault(e => e.Id == entryId) is { } entry)
        {
            if (!playlist.IsOwner)
            {
                return (403, "application/json", JsonBody(new { error = "forbidden" }));
            }

            // POST …/move-to-beginning — an den Listenanfang.
            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
                && segments[5].Equals("move-to-beginning", StringComparison.OrdinalIgnoreCase))
            {
                MoveToBeginningRequests.Add(entryId);
                playlist.Entries.Remove(entry);
                playlist.Entries.Insert(0, entry);
                return (200, "application/json", JsonBody(EntryJson(playlist, entry)));
            }

            // POST …/move-between — auf die Position der Ziel-SortOrder
            // (Listenindex); der Ziel-Eintrag rückt eine Position nach hinten.
            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase)
                && segments[5].Equals("move-between", StringComparison.OrdinalIgnoreCase))
            {
                var target = ReadLongProperty(ParseBody(body), "newSortOrder");
                MoveBetweenRequests.Add(entryId);
                MoveEntryToIndex(playlist, entry, target);
                return (204, "text/plain", Array.Empty<byte>());
            }

            // PUT …/order — direkte SortOrder-Setzung (Ans-Ende-Muster:
            // max + 1 landet am Listenende).
            if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
                && segments[5].Equals("order", StringComparison.OrdinalIgnoreCase))
            {
                var target = ReadLongProperty(ParseBody(body), "newSortOrder");
                ReorderEntryCount++;
                MoveEntryToIndex(playlist, entry, target);
                return (204, "text/plain", Array.Empty<byte>());
            }

            return (404, "application/json", JsonBody(new { error = "not found" }));
        }

        if (segments.Length == 4 && method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            return (200, "application/json", JsonBody(
                playlist.Entries.Select(e => EntryJson(playlist, e))));
        }

        // GET …/entries/max-sort-order — Listenindex des letzten Eintrags.
        if (segments.Length == 5
            && segments[4].Equals("max-sort-order", StringComparison.OrdinalIgnoreCase)
            && method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            return (200, "application/json", JsonBody(new
            {
                maxSortOrder = playlist.Entries.Count > 0 ? (long?)playlist.Entries.Count - 1 : null
            }));
        }

        // GET …/entries/paged — 1-basiertes Paging in Server-Reihenfolge.
        if (segments.Length == 5
            && segments[4].Equals("paged", StringComparison.OrdinalIgnoreCase)
            && method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            var pageNumber = int.TryParse(QueryValue(rawPath, "pageNumber"), out var pn) ? pn : 1;
            var pageSize = int.TryParse(QueryValue(rawPath, "pageSize"), out var ps) ? ps : 20;
            if (pageNumber < 1 || pageSize < 1)
            {
                return (400, "application/json", JsonBody(new { error = "invalid page" }));
            }

            var slice = playlist.Entries
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(e => EntryJson(playlist, e))
                .ToList();
            return (200, "application/json", JsonBody(new
            {
                entries = slice,
                totalCount = playlist.Entries.Count,
                hasNextPage = pageNumber * pageSize < playlist.Entries.Count,
                pageNumber,
                pageSize
            }));
        }

        return (404, "application/json", JsonBody(new { error = "not found" }));
    }

    /// <summary>
    /// Verschiebt einen Eintrag an die Ziel-Position (Listenindex =
    /// SortOrder); der Eintrag an der Ziel-Position rückt eine
    /// Position nach hinten.
    /// </summary>
    /// <param name="playlist">Die Playlist.</param>
    /// <param name="entry">Der zu verschiebende Eintrag.</param>
    /// <param name="targetSortOrder">Die Ziel-SortOrder (Listenindex).</param>
    private static void MoveEntryToIndex(MockPlaylist playlist, MockPlaylistEntry entry, long targetSortOrder)
    {
        playlist.Entries.Remove(entry);
        var index = (int)Math.Clamp(targetSortOrder, 0, playlist.Entries.Count);
        playlist.Entries.Insert(index, entry);
    }

    /// <summary>
    /// Konflikt-Antwort (HTTP 409) playlistbezogener Mutationen in der
    /// Feldform des Servers (ein gemeinsames Antwort-Schema).
    /// </summary>
    /// <param name="continueWatching"><c>true</c> = Weiterschauen-Bestätigung erforderlich.</param>
    /// <param name="lossOfData"><c>true</c> = Verlust-Bestätigung (manuelle Reihenfolge) erforderlich.</param>
    /// <param name="coverDeletion"><c>true</c> = Cover-Lösch-Bestätigung erforderlich (Vollständigkeit der Feldform).</param>
    /// <returns>Die 409-Antwort mit den Konflikt-Flaggen.</returns>
    private static MockResponse Conflict(
        bool continueWatching = false,
        bool lossOfData = false,
        bool coverDeletion = false) =>
        new(409, "application/json", JsonBody(new
        {
            isContinueWatchingConfirmationRequired = continueWatching,
            isLossOfDataConfirmationRequired = lossOfData,
            isCoverDeletionConfirmationRequired = coverDeletion
        }));

    /// <summary>
    /// Löst den Anzeigetitel eines Mediums für neu angelegte
    /// Playlist-Einträge auf: <see cref="KnownMedia"/> hat Vorrang,
    /// dann die bekannten Opt-in-Medien des Wiedergabeflusses,
    /// sonst <c>"Medium {id}"</c>.
    /// </summary>
    /// <param name="mediaType">Der Server-Medientyp.</param>
    /// <param name="mediaId">Die Medien-Id.</param>
    /// <returns>Der Anzeigetitel.</returns>
    private string ResolveMediaTitle(string mediaType, long mediaId)
    {
        var known = KnownMedia.FirstOrDefault(k =>
            k.MediaType.Equals(mediaType, StringComparison.OrdinalIgnoreCase) && k.MediaId == mediaId);
        if (known is not null)
        {
            return known.Title;
        }

        if (mediaId == PlaybackMovieId && mediaType.Equals("Movie", StringComparison.OrdinalIgnoreCase))
        {
            return PlaybackMovieTitle;
        }

        if (mediaId == PlaybackEpisodeId || mediaId == PlaybackNextEpisodeId)
        {
            return mediaId == PlaybackEpisodeId ? PlaybackEpisodeTitle : PlaybackNextEpisodeTitle;
        }

        if (mediaId == PlaybackShowId && mediaType.Equals("TVShow", StringComparison.OrdinalIgnoreCase))
        {
            return PlaybackShowTitle;
        }

        return $"Medium {mediaId}";
    }

    /// <summary>
    /// Liest den JSON-Anfragekörper eines Mutationsaufrufs; unlesbare
    /// Körper liefern ein leeres Objekt (Feld-Leser antworten dann
    /// mit Standardwerten).
    /// </summary>
    /// <param name="body">Der rohe Anfragekörper.</param>
    /// <returns>Das JSON-Wurzelelement.</returns>
    private static JsonElement ParseBody(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(body) ? "{}" : body);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            using var doc = JsonDocument.Parse("{}");
            return doc.RootElement.Clone();
        }
    }

    /// <summary>
    /// Liest Name, Beschreibung und Sortiermodus eines
    /// Playlist-Anlage/-Bearbeitungs-Körpers.
    /// </summary>
    /// <param name="body">Der rohe Anfragekörper.</param>
    /// <returns>Tupel (name, description, sortMode).</returns>
    private static (string, string?, string) ReadPlaylistBody(string body)
    {
        var root = ParseBody(body);
        return (
            ReadStringProperty(root, "name"),
            ReadStringProperty(root, "description") is { Length: > 0 } d ? d : null,
            ReadStringProperty(root, "sortMode"));
    }

    /// <summary>
    /// Antwortet die Playlist-Wiedergabe-Endpunkte
    /// (<c>POST /api/playlists/{id}/play</c> sowie
    /// <c>…/play/next|previous|advance</c>): <c>play</c> löst
    /// <paramref name="rawPath"/>s <c>entryId</c>-Parameter bzw. ohne
    /// Vorgabe den ersten abspielbaren und zugänglichen Eintrag der
    /// Server-Reihenfolge auf; die Navigations-Endpunkte liefern den
    /// nächsten/vorigen abspielbaren und zugänglichen Eintrag relativ
    /// zu <c>currentEntryId</c> — „kein Eintrag" geht als 204
    /// (Server-Konvention), ein unbekannter Eintrag als 404.
    /// Abspielbar sind Einträge vom Typ <c>Movie</c>/
    /// <c>TVShowEpisode</c> mit <see cref="MockPlaylistEntry.IsAccessible"/>.
    /// </summary>
    /// <param name="segments">Pfadsegmente des Requests.</param>
    /// <param name="playlist">Die aufgelöste Playlist.</param>
    /// <param name="rawPath">Der angefragte Pfad inkl. Query-String.</param>
    /// <returns>Die Mock-Antwort des Wiedergabe-Endpunkts.</returns>
    private MockResponse HandlePlaylistPlay(string[] segments, MockPlaylist playlist, string rawPath)
    {
        static bool IsPlayable(MockPlaylistEntry entry) =>
            entry.IsAccessible
            && (entry.MediaType.Equals("Movie", StringComparison.OrdinalIgnoreCase)
                || entry.MediaType.Equals("TVShowEpisode", StringComparison.OrdinalIgnoreCase));

        static string StreamMediaType(MockPlaylistEntry entry) =>
            entry.MediaType.Equals("Movie", StringComparison.OrdinalIgnoreCase)
                ? "movie"
                : "tvshowepisode";

        if (segments.Length == 4)
        {
            // POST /play — Start ab entryId bzw. erstem abspielbaren Eintrag.
            var entryId = long.TryParse(QueryValue(rawPath, "entryId"), out var eid) ? eid : (long?)null;
            var start = entryId is { } requestedId
                ? playlist.Entries.FirstOrDefault(e => e.Id == requestedId)
                : playlist.Entries.FirstOrDefault(IsPlayable);
            if (start is null || !IsPlayable(start))
            {
                return (404, "application/json", JsonBody(new { error = "not found" }));
            }

            return (200, "application/json", JsonBody(new
            {
                playlistId = playlist.Id,
                playlistName = playlist.Name,
                totalCount = playlist.Entries.Count,
                currentPosition = playlist.Entries.IndexOf(start) + 1,
                currentEntryId = start.Id,
                currentEntry = EntryJson(playlist, start),
                streamUrl = $"/api/items/{StreamMediaType(start)}/{start.MediaId}/stream",
                mediaType = StreamMediaType(start),
                mediaId = start.MediaId,
                startPositionSeconds = start.ResumePositionSeconds ?? 0L
            }));
        }

        if (segments.Length != 5)
        {
            return (404, "application/json", JsonBody(new { error = "not found" }));
        }

        var forward = segments[4].Equals("next", StringComparison.OrdinalIgnoreCase)
            || segments[4].Equals("advance", StringComparison.OrdinalIgnoreCase);
        if (!forward && !segments[4].Equals("previous", StringComparison.OrdinalIgnoreCase))
        {
            return (404, "application/json", JsonBody(new { error = "not found" }));
        }

        // next/previous/advance — nächster/voriger abspielbarer Eintrag
        // relativ zu currentEntryId in Server-Reihenfolge.
        var currentEntryId = long.TryParse(QueryValue(rawPath, "currentEntryId"), out var cid) ? cid : 0L;
        var currentIndex = playlist.Entries.FindIndex(e => e.Id == currentEntryId);
        if (currentIndex < 0)
        {
            return (404, "application/json", JsonBody(new { error = "not found" }));
        }

        var target = forward
            ? playlist.Entries.Skip(currentIndex + 1).FirstOrDefault(IsPlayable)
            : playlist.Entries.Take(currentIndex).LastOrDefault(IsPlayable);
        if (target is null)
        {
            return (204, "application/json", Array.Empty<byte>());
        }

        return (200, "application/json", JsonBody(new
        {
            entry = EntryJson(playlist, target),
            position = playlist.Entries.IndexOf(target) + 1,
            startPositionSeconds = target.ResumePositionSeconds ?? 0L
        }));
    }

    /// <summary>
    /// JSON-Repräsentation einer Playlist mit den DTO-Feldnamen;
    /// <c>allGenreIds</c> trägt die gesetzten Genre-Ids bzw. — solange
    /// die Testdaten keine zuordnen — abgeleitete Ids (100 + Index).
    /// </summary>
    /// <param name="playlist">Die Playlist.</param>
    /// <returns>Das serialisierbare Anonym-Objekt.</returns>
    private static object PlaylistJson(MockPlaylist playlist)
    {
        var genreIds = playlist.GenreIds.Count == playlist.Genres.Length
            ? playlist.GenreIds.ToArray()
            : playlist.Genres.Select((_, i) => (long)(100 + i)).ToArray();
        return new
        {
            id = playlist.Id,
            name = playlist.Name,
            description = playlist.Description,
            sortMode = playlist.SortMode,
            createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            updatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            genres = playlist.Genres.Select((name, i) => new { id = genreIds[i], name }),
            allGenreIds = genreIds,
            genresManuallyOverridden = playlist.GenresManuallyOverridden,
            coverPictureId = playlist.CoverPictureId,
            coverPictureIsUserUploaded = false,
            isPublic = playlist.IsPublic,
            isOwner = playlist.IsOwner
        };
    }

    /// <summary>JSON-Repräsentation eines Playlist-Eintrags mit den DTO-Feldnamen; <c>sortOrder</c> ist der Listenindex (Server-Reihenfolge des Mocks).</summary>
    /// <param name="playlist">Die zugehörige Playlist (Positionsbestimmung).</param>
    /// <param name="entry">Der Eintrag.</param>
    /// <returns>Das serialisierbare Anonym-Objekt.</returns>
    private static object EntryJson(MockPlaylist playlist, MockPlaylistEntry entry) => new
    {
        id = entry.Id,
        playlistId = playlist.Id,
        mediaType = entry.MediaType,
        mediaId = entry.MediaId,
        mediaTitle = entry.Title,
        parentMediaType = entry.ParentMediaType,
        parentMediaId = entry.ParentMediaId,
        parentMediaTitle = entry.ParentTitle,
        addedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        resolvedPictureId = entry.ResolvedPictureId,
        isAccessible = entry.IsAccessible,
        sortOrder = (long)playlist.Entries.IndexOf(entry),
        releaseDate = (DateTime?)null,
        plot = (string?)null,
        episodeNumber = entry.EpisodeNumber
    };

    /// <summary>
    /// Merkt einen per hide/skip entfernten Weiterschauen-Eintrag aus dem
    /// Anfragekörper (<c>{ mediaType, mediaId }</c>, Pascal- oder
    /// camelCase) vor — die nächste Listenantwort lässt ihn aus.
    /// </summary>
    /// <param name="body">Der JSON-Anfragekörper der Mutation.</param>
    private void RecordRemoval(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var mediaType = ReadStringProperty(doc.RootElement, "mediaType");
            var mediaId = ReadLongProperty(doc.RootElement, "mediaId");
            lock (RemovedContinueWatchingKeys)
            {
                RemovedContinueWatchingKeys.Add($"{mediaType}:{mediaId}");
            }
        }
        catch (Exception)
        {
            // Unlesbarer Body: der Request gilt dennoch als bedient —
            // Diagnose über die Request-Zähler.
        }
    }

    /// <summary>Prüft, ob ein Weiterschauen-Eintrag per hide/skip entfernt wurde.</summary>
    /// <param name="mediaType">Der Server-Medientyp des Eintrags.</param>
    /// <param name="mediaId">Die Medien-Id des Eintrags.</param>
    /// <returns><c>true</c>, wenn der Eintrag nicht mehr geliefert wird.</returns>
    private bool IsRemoved(string mediaType, long mediaId)
    {
        var key = $"{mediaType}:{mediaId}";
        lock (RemovedContinueWatchingKeys)
        {
            return RemovedContinueWatchingKeys.Contains(key);
        }
    }

    /// <summary>Liest eine numerische Eigenschaft case-insensitiv (Pascal- oder camelCase).</summary>
    /// <param name="root">Das JSON-Wurzelelement.</param>
    /// <param name="name">Der gesuchte Eigenschaftsname.</param>
    /// <returns>Der Wert oder <c>0</c>.</returns>
    private static long ReadLongProperty(JsonElement root, string name)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.Number
                && property.Value.TryGetInt64(out var value))
            {
                return value;
            }
        }

        return 0;
    }

    /// <summary>Liest eine boolesche Eigenschaft case-insensitiv.</summary>
    /// <param name="root">Das JSON-Wurzelelement.</param>
    /// <param name="name">Der gesuchte Eigenschaftsname.</param>
    /// <returns>Der Wert oder <c>false</c>.</returns>
    private static bool ReadBoolProperty(JsonElement root, string name)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return property.Value.GetBoolean();
            }
        }

        return false;
    }

    /// <summary>Liest ein Zahlen-Array case-insensitiv.</summary>
    /// <param name="root">Das JSON-Wurzelelement.</param>
    /// <param name="name">Der gesuchte Eigenschaftsname.</param>
    /// <returns>Die Zahlen des Arrays bzw. ein leeres Array.</returns>
    private static long[] ReadLongArrayProperty(JsonElement root, string name)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.Array)
            {
                return property.Value.EnumerateArray()
                    .Where(v => v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out _))
                    .Select(v => v.GetInt64())
                    .ToArray();
            }
        }

        return [];
    }

    /// <summary>
    /// Baut den JWT-artigen Administrator-Token (<see cref="LoginIsAdmin"/>):
    /// drei Base64Url-Abschnitte mit <c>IsAdmin = "True"</c>-Claim in
    /// der Payload — die App dekodiert die Payload ohne Signaturprüfung.
    /// </summary>
    /// <returns>Der Token im JWT-Format.</returns>
    private static string BuildAdminToken()
    {
        static string B64(string json) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var payload = B64("{\"sub\":\"1\",\"unique_name\":\"e2e\",\"email\":\"e2e@example.test\",\"IsAdmin\":\"True\"}");
        return $"{B64("{\"alg\":\"none\",\"typ\":\"JWT\"}")}.{payload}.e2e-signatur";
    }

    /// <summary>
    /// Medieneintrag im Server-JSON-Format (<c>$type</c>-Diskriminator
    /// der polymorphen MediaEntry-Hierarchie). Trägt optional die
    /// Serien-Einbettung (<c>season</c>/<c>show</c>) und die
    /// Staffel-/Episoden-Container (<c>seasons</c>/<c>episodes</c>) des
    /// Serienflusses; <c>null</c>-Felder werden nicht serialisiert.
    /// </summary>
    private sealed class PlaybackMediaEntryJson
    {
        [JsonPropertyName("$type")] public string Type { get; set; } = "movie";

        [JsonPropertyName("id")] public long Id { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

        [JsonPropertyName("mediaSourceId")] public long MediaSourceId { get; set; }

        [JsonPropertyName("number"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Number { get; set; }

        [JsonPropertyName("season"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PlaybackMediaEntryJson? Season { get; set; }

        [JsonPropertyName("show"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PlaybackMediaEntryJson? Show { get; set; }

        [JsonPropertyName("seasons"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PlaybackMediaEntryJson[]? Seasons { get; set; }

        [JsonPropertyName("episodes"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PlaybackMediaEntryJson[]? Episodes { get; set; }
    }

    /// <summary>
    /// <c>GET api/items/movie/{id}</c> (Movie-Details) und
    /// <c>GET api/items/movie/{id}/stream</c> (Videodaten mit minimalem
    /// Range-Support für den Seek: <c>Accept-Ranges</c>, <c>206 Partial
    /// Content</c> auf <c>Range</c>-Requests).
    /// </summary>
    /// <param name="path">Der angefragte Request-Pfad (ohne Query).</param>
    /// <param name="request">Die eingehende HTTP-Anfrage (für Header-Prüfung des Streams).</param>
    /// <returns>Die Mock-Antwort des Detail- bzw. Stream-Endpunkts.</returns>
    private MockResponse HandleMovieItem(
        string path,
        (string Method, string Path, Dictionary<string, string> Headers, string Body) request)
    {
        var rest = path["/api/items/movie/".Length..];
        if (rest.EndsWith("/stream", StringComparison.OrdinalIgnoreCase))
        {
            var idPart = rest[..^"/stream".Length];
            if (!long.TryParse(idPart, out var streamId)
                || (streamId != PlaybackMovieId && !IsPlaylistStreamMedia("Movie", streamId))
                || PlaybackStreamFails
                || PlaybackVideoPath is null
                || !File.Exists(PlaybackVideoPath))
            {
                return (404, "application/json", JsonBody(new { error = "not found" }));
            }

            return ServeVideo(request, File.ReadAllBytes(PlaybackVideoPath));
        }

        if (PlaybackMovieId is not { } movieId
            || !long.TryParse(rest, out var id)
            || id != movieId)
        {
            return (404, "application/json", JsonBody(new { error = "not found" }));
        }

        return (200, "application/json", JsonBody(new PlaybackMediaEntryJson
        {
            Id = movieId,
            Name = PlaybackMovieTitle,
            MediaSourceId = 1
        }));
    }

    /// <summary>
    /// Prüft, ob eine Medien-Id als abspielbarer Eintrag einer
    /// <see cref="MockPlaylist"/> streambar ist (Stream-Bedienung der
    /// Playlist-Wiedergabe ohne eigenes Opt-in — außer der Eintrag ist
    /// mit <see cref="MockPlaylistEntry.StreamUnavailable"/> markiert).
    /// </summary>
    /// <param name="serverMediaType">Der Server-Medientyp (<c>Movie</c>/<c>TVShowEpisode</c>).</param>
    /// <param name="mediaId">Die Medien-Id.</param>
    /// <returns><c>true</c>, wenn die Id als Playlist-Medium streambar ist.</returns>
    private bool IsPlaylistStreamMedia(string serverMediaType, long mediaId) =>
        Playlists.SelectMany(p => p.Entries).Any(e =>
            e.MediaId == mediaId
            && !e.StreamUnavailable
            && e.MediaType.Equals(serverMediaType, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// <c>GET api/SourceGenres/{id}</c>: liefert die per
    /// <see cref="SourceGenres"/> konfigurierte Genre-Liste der Quelle —
    /// nicht konfigurierte Quellen antworten mit einer leeren Liste
    /// (Nachbild des befundenen Serververhaltens „keine Genres").
    /// </summary>
    /// <param name="path">Der angefragte Request-Pfad (ohne Query).</param>
    /// <returns>Die Mock-Antwort des Genre-Endpunkts.</returns>
    private MockResponse HandleSourceGenres(string path)
    {
        var rest = path["/api/SourceGenres/".Length..];
        if (!long.TryParse(rest, out var sourceId) || sourceId <= 0)
        {
            return (404, "application/json", JsonBody(new { error = "not found" }));
        }

        var source = Sources.FirstOrDefault(s => s.Id == sourceId);
        SourceGenres.TryGetValue(sourceId, out var genres);
        return (200, "application/json", JsonBody(new
        {
            sourceId,
            sourceName = source?.Name ?? string.Empty,
            genres = (genres ?? []).Select(g => new { id = g.Id, name = g.Name }).ToArray()
        }));
    }

    /// <summary>
    /// <c>GET api/items?mediaSourceId=…&amp;page=…&amp;size=…[&amp;search=…][&amp;genreId=…]</c>:
    /// paginierte Eintragsliste der Quelle aus <see cref="SourceItems"/> —
    /// <c>genreId</c> filtert über <see cref="MockSourceItem.GenreIds"/>,
    /// <c>search</c> über den Titel (OrdinalIgnoreCase-Teilstring). Jeder
    /// Aufruf wird mit rohem Pfad (inkl. Query) in
    /// <see cref="SourceItemRequests"/> protokolliert.
    /// </summary>
    /// <param name="rawPath">Der angefragte Request-Pfad inklusive Query.</param>
    /// <returns>Die Mock-Antwort der Eintragsliste.</returns>
    private MockResponse HandleSourceItems(string rawPath)
    {
        lock (SourceItemRequests)
        {
            SourceItemRequests.Add(rawPath);
        }

        var mediaSourceId = long.TryParse(QueryValue(rawPath, "mediaSourceId"), out var msId) ? msId : 0L;
        var page = int.TryParse(QueryValue(rawPath, "page"), out var p) ? p : 0;
        var size = int.TryParse(QueryValue(rawPath, "size"), out var s) && s > 0 ? s : 20;
        var genreId = long.TryParse(QueryValue(rawPath, "genreId"), out var g) ? g : 0L;
        var search = QueryValue(rawPath, "search");

        var items = SourceItems
            .Where(i => i.MediaSourceId == mediaSourceId)
            .Where(i => genreId <= 0 || i.GenreIds.Contains(genreId))
            .Where(i => string.IsNullOrWhiteSpace(search)
                || i.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Skip(page * size)
            .Take(size);

        return (200, "application/json", JsonBody(items
            .Select(i => new { id = i.Id, type = i.Type, title = i.Title })
            .ToArray()));
    }

    /// <summary>
    /// <c>GET api/items/tvshow/{id}</c>: liefert die Opt-in-Serie mit
    /// einer Staffel, die Ausgangs-Episode (<see cref="PlaybackEpisodeId"/>)
    /// und die Folge-Episode (<see cref="PlaybackNextEpisodeId"/>)
    /// enthält. Dient auch der Detailansicht des nicht abspielbaren
    /// Weiterschauen-Serieneintrags (<see cref="ContinueWatchingShowId"/>).
    /// </summary>
    /// <param name="path">Der angefragte Request-Pfad (ohne Query).</param>
    /// <returns>Die Mock-Antwort des Serien-Endpunkts.</returns>
    private MockResponse HandleTvShowItem(string path)
    {
        var rest = path["/api/items/tvshow/".Length..];
        if (!long.TryParse(rest, out var id))
        {
            return (404, "application/json", JsonBody(new { error = "not found" }));
        }

        if (PlaybackShowId is { } showId && id == showId)
        {
            return (200, "application/json", JsonBody(new PlaybackMediaEntryJson
            {
                Type = "show",
                Id = showId,
                Name = PlaybackShowTitle,
                MediaSourceId = 1,
                Seasons =
                [
                    new PlaybackMediaEntryJson
                    {
                        Type = "season",
                        Id = 9001,
                        Name = "Staffel 1",
                        MediaSourceId = 1,
                        Number = 1,
                        Episodes =
                        [
                            new PlaybackMediaEntryJson
                            {
                                Type = "episode",
                                Id = PlaybackEpisodeId ?? 0,
                                Name = PlaybackEpisodeTitle,
                                MediaSourceId = 1,
                                Number = 1
                            },
                            new PlaybackMediaEntryJson
                            {
                                Type = "episode",
                                Id = PlaybackNextEpisodeId ?? 0,
                                Name = PlaybackNextEpisodeTitle,
                                MediaSourceId = 1,
                                Number = 2
                            }
                        ]
                    }
                ]
            }));
        }

        if (ContinueWatchingShowId is { } cwShowId && id == cwShowId)
        {
            return (200, "application/json", JsonBody(new PlaybackMediaEntryJson
            {
                Type = "show",
                Id = cwShowId,
                Name = ContinueWatchingShowTitle,
                MediaSourceId = 1,
                Seasons = ShowSeasons.Count > 0
                    ? ShowSeasons.Select(s => new PlaybackMediaEntryJson
                    {
                        Type = "season",
                        Id = s.Id,
                        Name = s.Name,
                        MediaSourceId = 1,
                        Number = s.Number,
                        Episodes = s.Episodes.Select(e => new PlaybackMediaEntryJson
                        {
                            Type = "episode",
                            Id = e.Id,
                            Name = e.Title,
                            MediaSourceId = 1,
                            Number = e.Number
                        }).ToArray()
                    }).ToArray()
                    : null
            }));
        }

        return (404, "application/json", JsonBody(new { error = "not found" }));
    }

    /// <summary>
    /// <c>GET api/items/tvshowepisode/{id}</c> (Episoden-Details) und
    /// <c>GET api/items/tvshowepisode/{id}/stream</c> (Videodaten wie
    /// beim Movie-Stream). Liefert Episoden des Opt-in-Serienflusses.
    /// </summary>
    /// <param name="path">Der angefragte Request-Pfad (ohne Query).</param>
    /// <param name="request">Die eingehende HTTP-Anfrage (für Header-Prüfung des Streams).</param>
    /// <returns>Die Mock-Antwort des Detail- bzw. Stream-Endpunkts.</returns>
    private MockResponse HandleEpisodeItem(
        string path,
        (string Method, string Path, Dictionary<string, string> Headers, string Body) request)
    {
        var rest = path["/api/items/tvshowepisode/".Length..];
        if (rest.EndsWith("/stream", StringComparison.OrdinalIgnoreCase))
        {
            var idPart = rest[..^"/stream".Length];
            if (!long.TryParse(idPart, out var streamId)
                || (streamId != PlaybackEpisodeId && streamId != PlaybackNextEpisodeId
                    && !IsPlaylistStreamMedia("TVShowEpisode", streamId))
                || PlaybackStreamFails
                || PlaybackVideoPath is null
                || !File.Exists(PlaybackVideoPath))
            {
                return (404, "application/json", JsonBody(new { error = "not found" }));
            }

            return ServeVideo(request, File.ReadAllBytes(PlaybackVideoPath));
        }

        if (!long.TryParse(rest, out var id)
            || (id != PlaybackEpisodeId && id != PlaybackNextEpisodeId))
        {
            return (404, "application/json", JsonBody(new { error = "not found" }));
        }

        return (200, "application/json", JsonBody(new PlaybackMediaEntryJson
        {
            Type = "episode",
            Id = id,
            Name = id == PlaybackEpisodeId ? PlaybackEpisodeTitle : PlaybackNextEpisodeTitle,
            MediaSourceId = 1,
            Number = id == PlaybackEpisodeId ? 1 : 2
        }));
    }

    /// <summary>
    /// Liefert die Videodatei: ganz (<c>200 OK</c>, <c>Accept-Ranges:
    /// bytes</c>) oder als Byte-Range (<c>206 Partial Content</c> mit
    /// <c>Content-Range</c>).
    /// </summary>
    /// <param name="request">Die eingehende HTTP-Anfrage (Range-Header).</param>
    /// <param name="video">Die abzuspielenden Videodaten.</param>
    /// <returns>Die Mock-Antwort (200 vollständig bzw. 206 Byte-Range).</returns>
    private static MockResponse ServeVideo(
        (string Method, string Path, Dictionary<string, string> Headers, string Body) request,
        byte[] video)
    {
        var headers = new Dictionary<string, string> { ["Accept-Ranges"] = "bytes" };
        if (request.Headers.TryGetValue("Range", out var rangeHeader)
            && TryParseRange(rangeHeader, video.Length, out var start, out var end))
        {
            var slice = new byte[end - start + 1];
            Array.Copy(video, (int)start, slice, 0, slice.Length);
            headers["Content-Range"] = $"bytes {start}-{end}/{video.Length}";
            return new MockResponse(206, "video/mp4", slice, headers);
        }

        return new MockResponse(200, "video/mp4", video, headers);
    }

    /// <summary>Liest einen Query-Parameter aus dem Request-Pfad.</summary>
    /// <param name="path">Der Request-Pfad mit Query-String.</param>
    /// <param name="name">Der gesuchte Query-Parametername.</param>
    /// <returns>Der URL-dekodierte Wert bzw. <c>null</c>, wenn nicht vorhanden.</returns>
    private static string? QueryValue(string path, string name)
    {
        var queryStart = path.IndexOf('?');
        if (queryStart < 0)
        {
            return null;
        }

        foreach (var pair in path[(queryStart + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(parts[1]);
            }
        }

        return null;
    }

    /// <summary>Parst <c>bytes=start-end</c>, <c>bytes=start-</c> und <c>bytes=-suffix</c>.</summary>
    /// <param name="header">Der Range-Header-Wert.</param>
    /// <param name="length">Gesamtlänge der Ressource in Bytes.</param>
    /// <param name="start">Ausgabeparameter: erstes Byte des Bereichs.</param>
    /// <param name="end">Ausgabeparameter: letztes Byte des Bereichs.</param>
    /// <returns><c>true</c>, wenn ein gültiger Bereich gelesen wurde.</returns>
    private static bool TryParseRange(string header, long length, out long start, out long end)
    {
        start = 0;
        end = length - 1;
        if (!header.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parts = header["bytes=".Length..].Split('-', 2);
        if (parts[0].Length > 0)
        {
            if (!long.TryParse(parts[0], out start))
            {
                return false;
            }

            end = parts.Length > 1 && long.TryParse(parts[1], out var explicitEnd)
                ? Math.Min(explicitEnd, length - 1)
                : length - 1;
        }
        else
        {
            // Suffix-Form „bytes=-N": die letzten N Bytes.
            if (parts.Length < 2 || !long.TryParse(parts[1], out var suffix))
            {
                return false;
            }

            start = Math.Max(0, length - suffix);
        }

        return start >= 0 && start < length && end >= start;
    }

    /// <summary>
    /// Gate-Ablehnung: 401 mit <c>application/problem+json</c> — daran
    /// erkennt die App-Probe <c>IsGateRejectedAsync</c> eine
    /// Gate-Ablehnung (Credential-Fehler kommen als
    /// <c>text/plain</c>).
    /// </summary>
    private static (int, string, byte[]) GateRejected() =>
        (401, "application/problem+json", JsonBody(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            title = "Unauthorized",
            status = 401
        }));

    /// <summary>Liest eine String-Eigenschaft case-insensitiv (Pascal- oder camelCase).</summary>
    /// <param name="root">Das JSON-Wurzelelement.</param>
    /// <param name="name">Der gesuchte Eigenschaftsname.</param>
    /// <returns>Der Wert oder ein leerer String.</returns>
    private static string ReadStringProperty(JsonElement root, string name)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : string.Empty;
            }
        }

        return string.Empty;
    }

    private static bool IsListEndpoint(string path) =>
        path.Equals("/api/favorites", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/api/continue-watching", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/api/Sources", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/api/items/recent", StringComparison.OrdinalIgnoreCase);

    private bool IsGateValid((string Method, string Path, Dictionary<string, string> Headers, string Body) request)
    {
        return request.Headers.TryGetValue("X-API-Key", out var key)
            && string.Equals(key.Trim(), DeviceToken, StringComparison.Ordinal);
    }

    private bool IsBearerValid((string Method, string Path, Dictionary<string, string> Headers, string Body) request)
    {
        return request.Headers.TryGetValue("Authorization", out var auth)
            && string.Equals(auth.Trim(), $"Bearer {AuthToken}", StringComparison.Ordinal);
    }

    private (int, string, byte[]) HandlePairingExchange(string body)
    {
        PairingExchangeCount++;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var code = root.GetProperty("code").GetString() ?? string.Empty;
            var clientPublicKey = root.GetProperty("clientPublicKey").GetString() ?? string.Empty;
            if (!string.Equals(code, PairingCode, StringComparison.Ordinal))
            {
                // 401 → InvalidPairingCodeError in der App.
                return (401, "text/plain", Encoding.UTF8.GetBytes("invalid code"));
            }

            var (serverPublicKey, encryptedToken) =
                MockPairingCrypto.EncryptToken(clientPublicKey, DeviceToken);
            return (200, "application/json", JsonBody(new
            {
                serverPublicKey,
                encryptedToken
            }));
        }
        catch (Exception)
        {
            return (400, "application/json", JsonBody(new { error = "bad request" }));
        }
    }

    /// <summary>
    /// <c>api/pairing/bootstrap</c>: löst das Ticket (langer Wert oder
    /// Kurzcode-Alias) ein und liefert das verschlüsselte Session-Paket —
    /// anonymer Endpunkt wie <c>exchange</c> (kein Gate, kein Bearer).
    /// </summary>
    /// <param name="body">Der JSON-Anfragekörper mit dem Ticket.</param>
    /// <returns>Status, MIME-Typ und Antwortkörper des Endpunkts.</returns>
    private (int, string, byte[]) HandleBootstrap(string body)
    {
        BootstrapCount++;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var ticket = root.GetProperty("ticket").GetString() ?? string.Empty;
            var clientPublicKey = root.GetProperty("clientPublicKey").GetString() ?? string.Empty;
            if (!string.Equals(ticket, BootstrapTicket, StringComparison.Ordinal)
                && !string.Equals(ticket, BootstrapShortCode, StringComparison.Ordinal))
            {
                // 401 → InvalidTicket in der App.
                return (401, "text/plain", Encoding.UTF8.GetBytes("invalid ticket"));
            }

            var payload = JsonSerializer.Serialize(new
            {
                deviceToken = DeviceToken,
                token = AuthToken,
                expires = DateTime.UtcNow.AddHours(12),
                refreshToken = RefreshToken
            });
            var (serverPublicKey, encryptedPayload) =
                MockPairingCrypto.EncryptPayload(clientPublicKey, payload);
            return (200, "application/json", JsonBody(new
            {
                serverPublicKey,
                encryptedPayload
            }));
        }
        catch (Exception)
        {
            return (400, "application/json", JsonBody(new { error = "bad request" }));
        }
    }

    /// <summary>
    /// <c>api/auth/refresh</c>: rotiert den Refresh-Token atomar —
    /// der alte Wert wird beim Antworten ungültig. Liegt hinter dem
    /// Client-Gate (wie der echte Server).
    /// </summary>
    /// <param name="request">Die eingehende HTTP-Anfrage (Refresh-Token im Body).</param>
    /// <returns>Status, MIME-Typ und Antwortkörper des Endpunkts.</returns>
    private (int, string, byte[]) HandleRefresh(
        (string Method, string Path, Dictionary<string, string> Headers, string Body) request)
    {
        if (!IsGateValid(request))
        {
            return GateRejected();
        }

        try
        {
            using var doc = JsonDocument.Parse(request.Body);
            var token = ReadStringProperty(doc.RootElement, "refreshToken");
            if (!string.Equals(token, RefreshToken, StringComparison.Ordinal))
            {
                // Ablehnung als Credential-Fehler (text/plain) — die
                // App-Probe trennt sie von der Gate-Ablehnung.
                return (401, "text/plain", Encoding.UTF8.GetBytes("invalid refresh token"));
            }

            RefreshToken = $"e2e-refresh-{Guid.NewGuid():N}";
            RefreshCount++;
            return (200, "application/json", JsonBody(new
            {
                token = AuthToken,
                expires = DateTime.UtcNow.AddHours(12),
                refreshToken = RefreshToken
            }));
        }
        catch (Exception)
        {
            return (400, "application/json", JsonBody(new { error = "bad request" }));
        }
    }

    /// <summary>
    /// <c>api/auth/logout</c>: widerruft den Refresh-Token — idempotent,
    /// antwortet auch bei bereits ungültigem Token mit 200.
    /// </summary>
    /// <param name="request">Die eingehende HTTP-Anfrage (Refresh-Token im Body).</param>
    /// <returns>Status, MIME-Typ und Antwortkörper des Endpunkts.</returns>
    private (int, string, byte[]) HandleLogout(
        (string Method, string Path, Dictionary<string, string> Headers, string Body) request)
    {
        LogoutCount++;
        try
        {
            using var doc = JsonDocument.Parse(request.Body);
            var token = ReadStringProperty(doc.RootElement, "refreshToken");
            if (string.Equals(token, RefreshToken, StringComparison.Ordinal))
            {
                RefreshToken = $"e2e-revoked-{Guid.NewGuid():N}";
            }
        }
        catch (Exception)
        {
            // Idempotent: auch ein unlesbarer Body ändert die 200 nicht.
        }

        return (200, "application/json", JsonBody(new { }));
    }

    private (int, string, byte[]) HandleLogin((string Method, string Path, Dictionary<string, string> Headers, string Body) request)
    {
        LastLoginRequestBody = request.Body;
        lock (LoginRequestBodies)
        {
            LoginRequestBodies.Add(request.Body);
        }
        LastLoginGateKey = request.Headers.TryGetValue("X-API-Key", out var key) ? key : null;

        // Client-Gate (MauiOnly-Semantik): ohne gültiges Geräte-Token
        // kommt keine Anmeldung durch — problem+json wie der
        // Action-Filter des echten Servers (Erkennungsmerkmal der
        // Gate-Probe der App).
        if (!IsGateValid(request))
        {
            return GateRejected();
        }

        try
        {
            using var doc = JsonDocument.Parse(request.Body);
            var root = doc.RootElement;
            // Der Client sendet PascalCase („Email“/“Password“); der
            // echte Server bindet case-insensitiv — der Mock auch.
            var email = ReadStringProperty(root, "email");
            var password = ReadStringProperty(root, "password");
            if (!string.Equals(email, Username, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(password, Password, StringComparison.Ordinal))
            {
                // Credential-Ablehnung: text/plain — die App unterscheidet
                // sie am Content-Type von der Gate-Ablehnung.
                return (401, "text/plain", Encoding.UTF8.GetBytes("invalid credentials"));
            }

            LoginCount++;
            return (200, "application/json", JsonBody(new
            {
                token = AuthToken,
                expires = DateTime.UtcNow.AddHours(12)
            }));
        }
        catch (Exception)
        {
            return (400, "application/json", JsonBody(new { error = "bad request" }));
        }
    }

    private static byte[] JsonBody(object payload) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

    /// <summary>
    /// Liest eine HTTP/1.1-Anfrage: Request-Line + Header bis zur
    /// Leerzeile, dann <c>Content-Length</c> Bytes Body.
    /// </summary>
    /// <param name="stream">Der Netzwerk-Stream der Client-Verbindung.</param>
    /// <param name="cancellationToken">Bricht das Lesen ab.</param>
    /// <returns>Die gelesene Anfrage bzw. <c>null</c> bei Verbindungsabbruch.</returns>
    private static async Task<(string Method, string Path, Dictionary<string, string> Headers, string Body)?> ReadRequestAsync(
        NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        var chunk = new byte[8192];
        var headerEnd = -1;
        while (headerEnd < 0)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
            headerEnd = IndexOf(buffer.GetBuffer(), (int)buffer.Length, "\r\n\r\n"u8);
            if (buffer.Length > 1_000_000)
            {
                return null;
            }
        }

        var headerText = Encoding.ASCII.GetString(buffer.GetBuffer(), 0, headerEnd);
        var lines = headerText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return null;
        }

        var parts = lines[0].Split(' ');
        if (parts.Length < 2)
        {
            return null;
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var contentLength = 0;
        foreach (var line in lines.Skip(1))
        {
            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var name = line[..colon].Trim();
            var value = line[(colon + 1)..].Trim();
            headers[name] = value;
            if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(value, out contentLength);
            }
        }

        var bodyStart = headerEnd + 4;
        var body = new MemoryStream();
        if (buffer.Length > bodyStart)
        {
            body.Write(buffer.GetBuffer(), bodyStart, (int)buffer.Length - bodyStart);
        }

        while (body.Length < contentLength)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0)
            {
                break;
            }

            body.Write(chunk, 0, read);
        }

        return (parts[0], parts[1], headers,
            Encoding.UTF8.GetString(body.GetBuffer(), 0, (int)Math.Min(body.Length, contentLength)));
    }

    private static int IndexOf(byte[] haystack, int length, ReadOnlySpan<byte> needle)
    {
        for (var i = 0; i + needle.Length <= length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    private static async Task WriteResponseAsync(
        NetworkStream stream, MockResponse response, CancellationToken cancellationToken)
    {
        var reason = response.Status switch
        {
            200 => "OK",
            204 => "No Content",
            206 => "Partial Content",
            400 => "Bad Request",
            401 => "Unauthorized",
            404 => "Not Found",
            429 => "Too Many Requests",
            _ => "Error"
        };
        var head = new StringBuilder(
            $"HTTP/1.1 {response.Status} {reason}\r\nContent-Type: {response.ContentType}\r\nContent-Length: {response.Body.Length}\r\n");
        if (response.Headers is not null)
        {
            foreach (var (name, value) in response.Headers)
            {
                head.Append($"{name}: {value}\r\n");
            }
        }

        head.Append("Connection: close\r\n\r\n");
        await stream.WriteAsync(Encoding.ASCII.GetBytes(head.ToString()), cancellationToken);
        await stream.WriteAsync(response.Body, cancellationToken);
    }
}

/// <summary>Opt-in-Playlist des Mock-Servers (Listen-, Detail-, Eintrags- und Cover-Antworten).</summary>
public sealed class MockPlaylist
{
    /// <summary>Id der Playlist.</summary>
    public long Id { get; set; }

    /// <summary>Name der Playlist.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Beschreibung der Playlist; <c>null</c> = keine.</summary>
    public string? Description { get; set; }

    /// <summary>Sortiermodus (<c>ByReleaseDate</c> oder <c>Manual</c>).</summary>
    public string SortMode { get; set; } = "Manual";

    /// <summary>Id des Cover-Bilds (steuert den Cover-Abruf der App); <c>null</c> = kein Cover.</summary>
    public long? CoverPictureId { get; set; }

    /// <summary>Bilddaten, die <c>GET /api/playlists/{id}/cover</c> ausliefert; <c>null</c> = 404.</summary>
    public byte[]? CoverPng { get; set; }

    /// <summary>Anzeige-Genres der Playlist (Namen).</summary>
    public string[] Genres { get; set; } = [];

    /// <summary>Die Playlist erscheint in <c>GET /api/playlists</c> (eigene Playlists).</summary>
    public bool IsOwner { get; set; } = true;

    /// <summary>Die Playlist erscheint in <c>GET /api/playlists/public</c>.</summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Genre-Ids zu <see cref="Genres"/> (Index-Parallelität) — tragen
    /// <c>allGenreIds</c> der JSON-Antwort, solange sie der
    /// <see cref="Genres"/>-Länge entsprechen; bei Abweichung werden
    /// abgeleitete Ids (100 + Index) ausgegeben.
    /// </summary>
    public List<long> GenreIds { get; } = [];

    /// <summary>Die Anzeige-Genres wurden manuell gesetzt (<c>genresManuallyOverridden</c> der Antwort).</summary>
    public bool GenresManuallyOverridden { get; set; }

    /// <summary>Einträge der Playlist — die Server-Reihenfolge (Listenindex = <c>sortOrder</c>).</summary>
    public List<MockPlaylistEntry> Entries { get; } = [];
}

/// <summary>Opt-in-Eintrag einer <see cref="MockPlaylist"/>.</summary>
public sealed class MockPlaylistEntry
{
    /// <summary>Id des Eintrags.</summary>
    public long Id { get; set; }

    /// <summary>Medientyp der Referenz (Server-Konvention, z. B. <c>Movie</c>, <c>TVShowEpisode</c>).</summary>
    public string MediaType { get; set; } = "Movie";

    /// <summary>Id des referenzierten Mediums.</summary>
    public long MediaId { get; set; }

    /// <summary>Anzeigetitel des Mediums.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Medientyp des Elternelements; <c>null</c> = kein Elternkontext.</summary>
    public string? ParentMediaType { get; set; }

    /// <summary>Id des Elternelements; <c>null</c> = kein Elternkontext.</summary>
    public long? ParentMediaId { get; set; }

    /// <summary>Titel des Elternelements; <c>null</c> = kein Elternkontext.</summary>
    public string? ParentTitle { get; set; }

    /// <summary>Serverseitig aufgelöste Bild-Id des Eintrags; <c>null</c> = kein Bild.</summary>
    public long? ResolvedPictureId { get; set; }

    /// <summary>Episodennummer bei Episoden-Einträgen; <c>null</c> sonst.</summary>
    public int? EpisodeNumber { get; set; }

    /// <summary>Der Benutzer hat Zugriff auf das Medium.</summary>
    public bool IsAccessible { get; set; } = true;

    /// <summary>
    /// Fortsetzungsposition des Eintrags in Sekunden
    /// (<c>startPositionSeconds</c> der Wiedergabe-Antworten);
    /// <c>null</c>/0 = Start am Anfang.
    /// </summary>
    public long? ResumePositionSeconds { get; set; }

    /// <summary>
    /// Simuliert einen fehlgeschlagenen Stream-Abruf des Mediums
    /// (<c>api/items/{typ}/{id}/stream</c> → 404) — das Medium bleibt
    /// dabei serverseitig ein abspielbarer Playlist-Eintrag; Grundlage
    /// des Fehler-Rückfall-Szenarios.
    /// </summary>
    public bool StreamUnavailable { get; set; }

    /// <summary>
    /// <c>true</c> = das Entfernen dieses Eintrags meldet ohne
    /// <c>confirmContinueWatchingRemoval</c>-Flagge den
    /// Weiterschauen-Konflikt (HTTP 409) — Nachbild der Server-Wache
    /// des Schritt-6-Flusses.
    /// </summary>
    public bool RequiresContinueWatchingRemovalConfirmation { get; set; }
}

/// <summary>Opt-in Genre-Option des Mock-Servers (<c>GET api/items/genres</c>).</summary>
public sealed class MockGenreOption
{
    /// <summary>Id des Genres.</summary>
    public long Id { get; set; }

    /// <summary>Anzeigename des Genres.</summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>Bekanntes Medium des Mock-Servers — Titel-Auflösung neu angelegter Playlist-Einträge.</summary>
public sealed class MockKnownMedia
{
    /// <summary>Medientyp (Server-Konvention, z. B. <c>Movie</c>).</summary>
    public string MediaType { get; set; } = "Movie";

    /// <summary>Id des Mediums.</summary>
    public long MediaId { get; set; }

    /// <summary>Anzeigetitel des Mediums.</summary>
    public string Title { get; set; } = string.Empty;
}

/// <summary>Staffel der Mock-Serie (<see cref="MockVideoServer.ShowSeasons"/>) — Serienansicht-E2E.</summary>
public sealed class MockShowSeason
{
    /// <summary>Id der Staffel.</summary>
    public long Id { get; set; }

    /// <summary>Staffelnummer (Sortier- und Anzeigeschlüssel).</summary>
    public int Number { get; set; }

    /// <summary>Anzeigename der Staffel (z. B. „Staffel 1").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Episoden der Staffel.</summary>
    public List<MockShowEpisode> Episodes { get; } = [];
}

/// <summary>Episode einer <see cref="MockShowSeason"/>.</summary>
public sealed class MockShowEpisode
{
    /// <summary>Id der Episode.</summary>
    public long Id { get; set; }

    /// <summary>Episodennummer innerhalb der Staffel.</summary>
    public int Number { get; set; }

    /// <summary>Anzeigetitel der Episode.</summary>
    public string Title { get; set; } = string.Empty;
}

/// <summary>Opt-in-Quelle des Mock-Servers (<c>GET /api/Sources</c>, <see cref="MockVideoServer.Sources"/>).</summary>
public sealed class MockSource
{
    /// <summary>Id der Quelle.</summary>
    public long Id { get; set; }

    /// <summary>Anzeigename der Quelle (Menüeintrag, Seitentitel).</summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>Genre-Eintrag einer Quelle (<c>GET api/SourceGenres/{id}</c>, <see cref="MockVideoServer.SourceGenres"/>).</summary>
public sealed class MockSourceGenre
{
    /// <summary>Id des Genres.</summary>
    public long Id { get; set; }

    /// <summary>Anzeigename des Genres.</summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Opt-in-Eintrag des Quellen-Browsing (<see cref="MockVideoServer.SourceItems"/>,
/// <c>GET api/items?mediaSourceId=…</c>).
/// </summary>
public sealed class MockSourceItem
{
    /// <summary>Id des Eintrags.</summary>
    public long Id { get; set; }

    /// <summary>Anzeigetitel des Eintrags.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Medientyp der Summary (Server-Konvention, z. B. <c>Movie</c>, <c>TvShow</c>).</summary>
    public string Type { get; set; } = "Movie";

    /// <summary>Id der Quelle, der der Eintrag gehört (<c>mediaSourceId</c>-Filter).</summary>
    public long MediaSourceId { get; set; }

    /// <summary>Genre-Ids des Eintrags — Grundlage des <c>genreId</c>-Filters.</summary>
    public List<long> GenreIds { get; } = [];
}
