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
        AuthToken = $"e2e-auth-{Guid.NewGuid():N}";
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

    /// <summary>Das ausgestellte Anmelde-Token.</summary>
    public string AuthToken { get; }

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
    /// Opt-in nicht abspielbarer Weiterschauen-Eintrag: Id eines
    /// Serieneintrags (<c>mediaType: "show"</c>) — öffnet die
    /// Detailansicht statt der Wiedergabe.
    /// </summary>
    public long? ContinueWatchingShowId { get; set; }

    /// <summary>Anzeigetitel des nicht abspielbaren Weiterschauen-Serieneintrags.</summary>
    public string ContinueWatchingShowTitle { get; set; } = "E2E-Serie";

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
                    title = PlaybackMovieTitle
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
                    title = PlaybackEpisodeTitle
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
        if (PlaybackMovieId is not { } movieId)
        {
            return (404, "application/json", JsonBody(new { error = "not found" }));
        }

        var rest = path["/api/items/movie/".Length..];
        if (rest.EndsWith("/stream", StringComparison.OrdinalIgnoreCase))
        {
            var idPart = rest[..^"/stream".Length];
            if (!long.TryParse(idPart, out var streamId)
                || streamId != movieId
                || PlaybackStreamFails
                || PlaybackVideoPath is null
                || !File.Exists(PlaybackVideoPath))
            {
                return (404, "application/json", JsonBody(new { error = "not found" }));
            }

            return ServeVideo(request, File.ReadAllBytes(PlaybackVideoPath));
        }

        if (!long.TryParse(rest, out var id) || id != movieId)
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
                MediaSourceId = 1
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
                || (streamId != PlaybackEpisodeId && streamId != PlaybackNextEpisodeId)
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
