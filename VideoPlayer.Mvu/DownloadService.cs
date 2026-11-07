using System.Collections.Concurrent;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VideoPlayer.Storage;
using VideoPlayer.Storage.Entities;

namespace VideoPlayer.Mvu;

/// <summary>Ereignisdaten für eine Änderung eines Download-Eintrags.</summary>
public sealed class DownloadItemEventArgs : EventArgs
{
    /// <summary>Der geänderte Download-Eintrag (Snapshot).</summary>
    /// <value>Der geänderte Eintrag.</value>
    public required DownloadListItem Item { get; init; }
}

/// <summary>Ereignisdaten für das Entfernen eines Download-Eintrags.</summary>
public sealed class DownloadRemovedEventArgs : EventArgs
{
    /// <summary>Medientyp des entfernten Downloads (normalisiert).</summary>
    /// <value>Der Medientyp.</value>
    public required string MediaType { get; init; }

    /// <summary>Id des Mediums des entfernten Downloads.</summary>
    /// <value>Die Medien-Id.</value>
    public required long MediaId { get; init; }
}

/// <summary>Ergebnis einer Download-Einreihe-Anforderung.</summary>
public sealed record DownloadEnqueueResult
{
    /// <summary>Der zugehörige Manifest-Eintrag als Anzeige-Snapshot; <c>null</c>, wenn die Anforderung ungültig war.</summary>
    public DownloadListItem? Item { get; init; }

    /// <summary>Dem Anwender anzeigbarer Hinweistext (Erfolgs- oder Fehlerhinweis); <c>null</c> = kein Hinweis.</summary>
    public string? Notice { get; init; }

    /// <summary><c>true</c>, wenn der Hinweis eine Ablehnung/einen Fehler beschreibt.</summary>
    public bool IsError { get; init; }
}

/// <summary>
/// Download-Verwaltung (Service Layer): führt Download-Aufträge als
/// persistente Zeilen des Download-Manifests (<c>download_manifest</c>)
/// aus und legt die Videodateien unter
/// <see cref="AppStorageLayout.VideosDirectory"/> (nicht backup-pflichtig)
/// ab. Es laufen bis zu <see cref="MaxParallelDownloads"/> Transfers
/// parallel; wartende Aufträge werden in Manifest-Reihenfolge gestartet.
/// Fortschritt und Zustandswechsel werden als <see cref="ItemChanged"/>-
/// Ereignisse gemeldet (der Effekt-Handler speist sie als
/// <c>DownloadItemChangedMessage</c> in den MVU-Zyklus).
/// <para>
/// Der Server-Stream <c>api/items/{typ}/{id}/stream</c> unterstützt keine
/// Bereichsanfragen (Range) — die automatische Wiederaufnahme
/// (<see cref="ResumeInterruptedAsync"/>) startet unterbrochene Downloads
/// daher erneut ab Byte 0.
/// </para>
/// </summary>
public sealed class DownloadService
{
    /// <summary>Maximale Anzahl gleichzeitig laufender Download-Transfers.</summary>
    public const int MaxParallelDownloads = 2;

    /// <summary>Fehlercode: Speicherplatzkontingent erreicht.</summary>
    public const string ErrorQuotaExceeded = "quota_exceeded";

    /// <summary>Fehlercode: keine Serververbindung.</summary>
    public const string ErrorNoConnection = "no_connection";

    /// <summary>Fehlercode: Netzwerk-/Transportfehler (auch Zeitüberschreitung).</summary>
    public const string ErrorNetwork = "network";

    /// <summary>Fehlercode: lokaler Dateisystemfehler.</summary>
    public const string ErrorIo = "io";

    /// <summary>Fehlercode-Präfix: HTTP-Fehlerstatus des Servers (<c>http_&lt;code&gt;</c>).</summary>
    public const string ErrorHttpPrefix = "http_";

    /// <summary>Fehlercode: als abgeschlossen geführter Download ohne lokale Datei (durch Abgleich erkannt).</summary>
    public const string ErrorMissingFile = "missing_file";

    /// <summary>Hinweis: der Download ist bereits abgeschlossen vorhanden.</summary>
    public const string NoticeAlreadyPresent = "Der Download ist bereits vorhanden.";

    /// <summary>Hinweis: der Download läuft bereits.</summary>
    public const string NoticeAlreadyRunning = "Der Download läuft bereits.";

    /// <summary>Hinweis: der Download wartet bereits auf seinen Start.</summary>
    public const string NoticeAlreadyQueued = "Der Download wartet bereits.";

    /// <summary>Hinweis: der Download wurde eingeplant.</summary>
    public const string NoticeEnqueued = "Der Download wurde eingeplant.";

    /// <summary>Fehlertext bei Erreichen des Speicherplatzkontingents.</summary>
    public const string NoticeQuotaExceeded = "Das Speicherplatzkontingent ist erreicht. Downloads löschen oder das Kontingent erhöhen.";

    /// <summary>Fehlertext bei ungültiger Download-Anforderung.</summary>
    public const string NoticeInvalidMedia = "Dieses Medium kann nicht heruntergeladen werden.";

    /// <summary>
    /// Faktor des Wiedergabe-Schutzes: das Ablaufdatum eines lokal
    /// wiedergegebenen Downloads wird auf <c>jetzt + Faktor × Restdauer</c>
    /// hochgesetzt (<see cref="ExtendExpiryAsync"/>).
    /// </summary>
    public const int PlaybackProtectionFactor = 2;

    private const long BytesPerGigabyte = 1_073_741_824L;
    private const int CopyBufferSize = 81920;

    /// <summary>
    /// Chunks zwischen zwei Neumessungen der Verzeichnisbelegung im
    /// Transfer (8 × <see cref="CopyBufferSize"/> ≈ 640 KiB).
    /// </summary>
    private const int QuotaScanIntervalChunks = 8;
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(250);

    private readonly AuthSessionService authSession;
    private readonly AppSecretsService secrets;
    private readonly CacheDatabase cache;
    private readonly AppStorageLayout layout;
    private readonly INetworkStatusMonitor networkMonitor;
    private readonly Func<HttpClient> httpClientFactory;
    private readonly Func<string, bool> fileExists;
    private readonly long? quotaBytesOverride;
    private readonly IDownloadBackend? backend;
    private readonly ILogger<DownloadService> logger;
    private readonly Func<DateTime> utcNow;

    private readonly SemaphoreSlim manifestGate = new(1, 1);
    private readonly SemaphoreSlim pumpGate = new(1, 1);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> runningTransfers = new();
    private readonly ConcurrentDictionary<string, DateTime> lastProgressEmit = new();

    /// <summary>Erstellt den Dienst mit der produktiven HTTP- und Datei-Behandlung.</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client und der Server-Basisadresse.</param>
    /// <param name="secrets">Ablage des Anmelde-Tokens (Fallback, wenn kein Client angemeldet ist).</param>
    /// <param name="cache">Cache-Ablage mit dem Download-Manifest.</param>
    /// <param name="layout">Ablagelayout mit dem Video-Verzeichnis.</param>
    /// <param name="networkMonitor">Netzwerkstatus-Erkennung (WLAN-Bedingung).</param>
    /// <param name="logger">Optionaler Logger für das Programmprotokoll.</param>
    /// <param name="backend">Plattform-Transfer-Backend (iOS: Hintergrund-Session); <c>null</c> = prozessinterner HTTP-Stream.</param>
    public DownloadService(
        AuthSessionService authSession,
        AppSecretsService secrets,
        CacheDatabase cache,
        AppStorageLayout layout,
        INetworkStatusMonitor networkMonitor,
        ILogger<DownloadService>? logger = null,
        IDownloadBackend? backend = null)
        : this(authSession, secrets, cache, layout, networkMonitor, null, null, null, logger, backend, null)
    {
    }

    /// <summary>Erstellt den Dienst mit injizierbaren Implementierungen (für Tests).</summary>
    /// <param name="authSession">Sitzungsdienst mit dem angemeldeten API-Client und der Server-Basisadresse.</param>
    /// <param name="secrets">Ablage des Anmelde-Tokens.</param>
    /// <param name="cache">Cache-Ablage mit dem Download-Manifest.</param>
    /// <param name="layout">Ablagelayout mit dem Video-Verzeichnis.</param>
    /// <param name="networkMonitor">Netzwerkstatus-Erkennung (WLAN-Bedingung).</param>
    /// <param name="httpClientFactory">Fabrik für den Download-<see cref="HttpClient"/>; <c>null</c> = produktive Erzeugung ohne Timeout.</param>
    /// <param name="fileExists">Existenzprüfung für lokale Dateien; <c>null</c> = <see cref="File.Exists(string?)"/>.</param>
    /// <param name="quotaBytesOverride">Ersetzt das GB-Kontingent der Einstellungen (für Tests); <c>null</c> = <c>DownloadQuotaGb</c>.</param>
    /// <param name="logger">Optionaler Logger für das Programmprotokoll.</param>
    /// <param name="backend">Plattform-Transfer-Backend (iOS: Hintergrund-Session); <c>null</c> = prozessinterner HTTP-Stream.</param>
    /// <param name="utcNow">Zeitquelle der Ablauflogik (für Tests); <c>null</c> = <see cref="DateTime.UtcNow"/>.</param>
    internal DownloadService(
        AuthSessionService authSession,
        AppSecretsService secrets,
        CacheDatabase cache,
        AppStorageLayout layout,
        INetworkStatusMonitor networkMonitor,
        Func<HttpClient>? httpClientFactory,
        Func<string, bool>? fileExists,
        long? quotaBytesOverride,
        ILogger<DownloadService>? logger = null,
        IDownloadBackend? backend = null,
        Func<DateTime>? utcNow = null)
    {
        this.authSession = authSession ?? throw new ArgumentNullException(nameof(authSession));
        this.secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
        this.networkMonitor = networkMonitor ?? throw new ArgumentNullException(nameof(networkMonitor));
        this.httpClientFactory = httpClientFactory ?? CreateHttpClient;
        this.fileExists = fileExists ?? File.Exists;
        this.quotaBytesOverride = quotaBytesOverride;
        this.logger = logger ?? NullLogger<DownloadService>.Instance;
        // Backend delegiert den Transfer (iOS: systemseitige Hintergrund-
        // Session). Ohne Angabe bleibt der prozessinterne HTTP-Stream.
        this.backend = backend;
        this.utcNow = utcNow ?? (() => DateTime.UtcNow);
    }

    /// <summary>Wird ausgelöst, wenn ein Download-Eintrag geändert wurde (Zustand oder Fortschritt).</summary>
    public event EventHandler<DownloadItemEventArgs>? ItemChanged;

    /// <summary>Wird ausgelöst, wenn ein Download-Eintrag entfernt wurde.</summary>
    public event EventHandler<DownloadRemovedEventArgs>? ItemRemoved;

    /// <summary>
    /// Liest alle Download-Einträge des Manifests (in Auftragsreihenfolge)
    /// als Anzeige-Snapshots.
    /// </summary>
    /// <param name="cancellationToken">Bricht das Lesen ab.</param>
    /// <returns>Die Download-Einträge des Manifests.</returns>
    public async Task<IReadOnlyList<DownloadListItem>> LoadItemsAsync(CancellationToken cancellationToken = default)
    {
        var entries = await cache.Connection.Table<DownloadManifestEntry>().ToListAsync();
        return entries
            .OrderBy(e => e.Id)
            .Select(ToItem)
            .ToList();
    }

    /// <summary>
    /// Belegter Download-Speicher in Bytes — die Summe der Dateigrößen im
    /// Video-Verzeichnis (inkl. verwaister Dateien ohne Manifest-Eintrag).
    /// </summary>
    /// <returns>Die belegten Bytes; 0 bei nicht lesbarem Verzeichnis.</returns>
    public long GetUsedBytes()
    {
        try
        {
            return Directory
                .EnumerateFiles(layout.VideosDirectory)
                .Sum(path => new FileInfo(path).Length);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Nicht lesbares Verzeichnis → konservativ 0 (die Engine
            // prüft ohnehin beim Schreiben erneut).
            logger.LogWarning(ex, "Speicherbelegung nicht lesbar.");
            return 0;
        }
    }

    /// <summary>
    /// Reiht einen Download ein (bzw. setzt einen fehlgeschlagenen/
    /// abgebrochenen Auftrag zurück) und startet den Transfer, sobald ein
    /// Platz frei ist. Bestehende Einträge desselben Mediums werden
    /// dedupliziert: abgeschlossene Downloads mit vorhandener Datei und
    /// bereits wartende/laufende Aufträge werden nicht erneut angelegt.
    /// Bei Erreichen des Speicherplatzkontingents wird der Eintrag als
    /// fehlgeschlagen (<see cref="ErrorQuotaExceeded"/>) geführt.
    /// </summary>
    /// <param name="request">Die Einreihe-Anforderung (Medientyp wird normalisiert; nur <c>movie</c>/<c>tvshowepisode</c> sind ladbar).</param>
    /// <param name="settings">Aktuelle App-Einstellungen (Kontingent, WLAN-Bedingung).</param>
    /// <param name="cancellationToken">Bricht die Manifest-Operation ab (nicht den Transfer).</param>
    /// <returns>Das Einreihe-Ergebnis mit Snapshot und Hinweistext.</returns>
    public async Task<DownloadEnqueueResult> EnqueueAsync(
        DownloadEnqueueRequest request,
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(settings);
        var type = MediaTypeNames.Normalize(request.MediaType);
        var mediaId = request.MediaId;
        var title = request.Title;

        if (mediaId <= 0 || type is not ("movie" or "tvshowepisode"))
        {
            return LogEnqueueResult(type, mediaId, title, new DownloadEnqueueResult
            {
                Item = null,
                Notice = NoticeInvalidMedia,
                IsError = true
            });
        }

        // Ab hier trägt die Anforderung den normalisierten Medientyp.
        request = request with { MediaType = type };
        DownloadManifestEntry? entry = null;
        DownloadEnqueueResult? dedup = null;
        DownloadListItem? changedItem = null;
        await manifestGate.WaitAsync(cancellationToken);
        try
        {
            var existing = await FindEntryAsync(type, mediaId);
            if (existing is not null)
            {
                (dedup, entry, changedItem) = await ResolveExistingEntryAsync(existing, request, settings);
            }
            else
            {
                entry = CreateManifestEntry(request);
                await cache.Connection.InsertAsync(entry);
            }
        }
        finally
        {
            manifestGate.Release();
        }

        // Das ItemChanged-Ereignis einer Dedup-Verlängerung wird erst
        // nach dem Gate-Release ausgelöst — der kritische Abschnitt
        // bleibt kurz und Handler laufen nicht in das noch gehaltene
        // Gate (Muster UpdateManifestEntryAsync).
        if (changedItem is not null)
        {
            RaiseItemChanged(changedItem);
        }

        if (dedup is not null)
        {
            return LogEnqueueResult(type, mediaId, title, dedup);
        }

        // Vorab-Quota-Prüfung: ist das Kontingent bereits belegt, wird der
        // Auftrag sofort als fehlgeschlagen geführt statt zu warten.
        var queued = entry!;
        if (await FailIfQuotaExceededAsync(queued, settings))
        {
            return new DownloadEnqueueResult
            {
                Item = ToItem(queued),
                Notice = NoticeQuotaExceeded,
                IsError = true
            };
        }

        RaiseItemChanged(ToItem(queued));
        PumpQueue(settings);
        return LogEnqueueResult(type, mediaId, title, new DownloadEnqueueResult { Item = ToItem(queued), Notice = NoticeEnqueued });
    }

    /// <summary>
    /// Protokolliert das Einreihe-Ergebnis und liefert es zurück:
    /// Fehler-Notices (ungültiges Medium, Kontingent erschöpft) sind
    /// überholbare Einzelfehler → Warning; nur die tatsächliche
    /// Einplanung ist ein Verlaufsereignis (Information). Dedup-Hinweise
    /// ohne Zustandsänderung (AlreadyRunning/AlreadyQueued/
    /// AlreadyPresent) bleiben Debug — Auto-Download-Läufe prüfen pro
    /// Kandidat und würden sonst pro Eintrag der Liste einen Eintrag
    /// ohne Vorgang erzeugen.
    /// </summary>
    /// <param name="type">Der normalisierte Medientyp.</param>
    /// <param name="mediaId">Die Medien-Id.</param>
    /// <param name="title">Der Anzeigetitel des Downloads.</param>
    /// <param name="result">Das zu protokollierende Ergebnis.</param>
    /// <returns>Das übergebene Ergebnis.</returns>
    private DownloadEnqueueResult LogEnqueueResult(
        string type, long mediaId, string? title, DownloadEnqueueResult result)
    {
        if (result.IsError)
        {
            logger.LogWarning(
                "Download-Auftrag {MediaType}/{MediaId} ({Title}): {Notice}",
                type,
                mediaId,
                title,
                result.Notice);
        }
        else if (result.Notice == NoticeEnqueued)
        {
            logger.LogInformation(
                "Download-Auftrag {MediaType}/{MediaId} ({Title}): {Notice}",
                type,
                mediaId,
                title,
                result.Notice);
        }
        else
        {
            logger.LogDebug(
                "Download-Auftrag {MediaType}/{MediaId} ({Title}): {Notice}",
                type,
                mediaId,
                title,
                result.Notice);
        }

        return result;
    }

    /// <summary>
    /// Dedup- und Requeue-Behandlung eines vorhandenen Manifest-Eintrags
    /// (läuft unter dem <see cref="manifestGate"/> des Aufrufers):
    /// laufende, wartende und abgeschlossene Aufträge liefern ihr
    /// Dedup-Ergebnis — im Auto-Download-Vorlauf wird ein bereits
    /// vorhandener Kandidat verlängert statt erneut geladen
    /// (Ablaufdatum + Herkunft „auto"). Fehlgeschlagene, abgebrochene
    /// und verwaiste Einträge (completed ohne Datei, downloading ohne
    /// laufenden Transfer) werden als wartend erneut eingeplant
    /// (<c>Requeued</c> des Ergebnistupels); Herkunft und Kontext
    /// bleiben erhalten, das bisherige Ablaufdatum wird zurückgesetzt.
    /// </summary>
    /// <param name="existing">Der vorhandene Manifest-Eintrag.</param>
    /// <param name="request">Die Einreihe-Anforderung (Titel, Kontexte, Bild-Id, Herkunft).</param>
    /// <param name="settings">Aktuelle App-Einstellungen (Aufbewahrungszeit der Dedup-Verlängerung).</param>
    /// <returns>
    /// Das Dedup-Ergebnis bzw. der erneut eingeplante Eintrag (genau
    /// eines von beiden) sowie der Änderungs-Snapshot einer
    /// Dedup-Verlängerung — das <see cref="ItemChanged"/>-Ereignis
    /// löst der Aufrufer erst nach dem Gate-Release aus.
    /// </returns>
    private async Task<(DownloadEnqueueResult? Result, DownloadManifestEntry? Requeued, DownloadListItem? Changed)>
        ResolveExistingEntryAsync(
            DownloadManifestEntry existing,
            DownloadEnqueueRequest request,
            AppSettings settings)
    {
        var state = DownloadStateNames.FromPersisted(existing.State);
        if (state == DownloadState.Downloading
            && runningTransfers.ContainsKey(ManifestKey(existing.VideoType, existing.VideoId)))
        {
            return (new DownloadEnqueueResult { Item = ToItem(existing), Notice = NoticeAlreadyRunning }, null, null);
        }
        if (state == DownloadState.Queued)
        {
            return (new DownloadEnqueueResult { Item = ToItem(existing), Notice = NoticeAlreadyQueued }, null, null);
        }
        if (state == DownloadState.Completed && fileExists(DownloadManifestPaths.Resolve(layout, existing)))
        {
            // Dedup im Auto-Download-Vorlauf: ein bereits vorhandener
            // Kandidat wird verlängert statt erneut geladen — Ablauf-
            // datum auf jetzt + Aufbewahrungszeit hochsetzen und die
            // Herkunft auf „auto" stellen (auch ein manueller Download
            // wird damit zum automatischen; UI-Bezeichnung „Cache").
            DownloadListItem? changed = null;
            if (request.Origin == DownloadOrigin.Auto)
            {
                existing.ExpiresAtUtc = utcNow() + TimeSpan.FromDays(settings.DownloadRetentionDays);
                existing.Origin = DownloadOriginNames.ToPersisted(DownloadOrigin.Auto);
                existing.UpdatedAtUtc = utcNow();
                await cache.Connection.UpdateAsync(existing);
                changed = ToItem(existing);
            }

            return (new DownloadEnqueueResult { Item = ToItem(existing), Notice = NoticeAlreadyPresent }, null, changed);
        }

        existing.State = DownloadStateNames.ToPersisted(DownloadState.Queued);
        existing.Error = null;
        existing.DownloadedBytes = 0;
        existing.TotalBytes = 0;
        existing.ExpiresAtUtc = null;
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            existing.Title = request.Title;
        }
        ApplySeriesContext(existing, request.SeriesContext);
        ApplyPlaylistContext(existing, request.PlaylistContext);
        existing.PictureId ??= request.PictureId;
        existing.UpdatedAtUtc = utcNow();
        await cache.Connection.UpdateAsync(existing);
        return (null, existing, null);
    }

    /// <summary>
    /// Erstellt den Manifest-Eintrag einer neuen Einreihe-Anforderung
    /// (Zustand wartend, Herkunft des Auftrags, Kontextangaben) — noch
    /// nicht persistiert.
    /// </summary>
    /// <param name="request">Die Einreihe-Anforderung (Typ, Id, Titel, Kontexte, Bild-Id, Herkunft).</param>
    /// <returns>Der neue Manifest-Eintrag.</returns>
    private DownloadManifestEntry CreateManifestEntry(DownloadEnqueueRequest request)
    {
        var entry = new DownloadManifestEntry
        {
            VideoId = (int)request.MediaId,
            VideoType = request.MediaType,
            Title = request.Title ?? string.Empty,
            FilePath = FilePathFor(request.MediaType, request.MediaId),
            State = DownloadStateNames.ToPersisted(DownloadState.Queued),
            Origin = DownloadOriginNames.ToPersisted(request.Origin),
            UpdatedAtUtc = utcNow()
        };
        ApplySeriesContext(entry, request.SeriesContext);
        ApplyPlaylistContext(entry, request.PlaylistContext);
        entry.PictureId ??= request.PictureId;
        return entry;
    }

    /// <summary>
    /// Führt den Auftrag als fehlgeschlagen (<see cref="ErrorQuotaExceeded"/>),
    /// wenn das Speicherplatzkontingent bereits belegt ist.
    /// </summary>
    /// <param name="entry">Der eingeplante Manifest-Eintrag.</param>
    /// <param name="settings">Aktuelle App-Einstellungen (Kontingent).</param>
    /// <returns><c>true</c>, wenn das Kontingent erschöpft ist und der Eintrag als fehlgeschlagen geführt wurde.</returns>
    private async Task<bool> FailIfQuotaExceededAsync(DownloadManifestEntry entry, AppSettings settings)
    {
        if (GetUsedBytes() < QuotaBytesFor(settings))
        {
            return false;
        }

        // Der Zustandswechsel-Eintrag von SetStateAsync (Failed mit
        // quota_exceeded) trägt das Ereignis bereits als Warnung —
        // kein zweiter Warn-Eintrag mit dem Hinweistext für
        // denselben Vorgang.
        await SetStateAsync(entry, DownloadState.Failed, ErrorQuotaExceeded);
        return true;
    }

    /// <summary>
    /// Überträgt den mitgegebenen Serienkontext in den Manifesteintrag —
    /// nur für Episoden-Downloads und nur, wenn Werte bekannt sind
    /// (vorhandene Werte eines Re-Enqueue werden nicht zurückgesetzt).
    /// </summary>
    /// <param name="entry">Der zu ergänzende Manifesteintrag.</param>
    /// <param name="seriesContext">Der Serienkontext; <c>null</c> = keine Änderung.</param>
    private static void ApplySeriesContext(DownloadManifestEntry entry, DownloadSeriesContext? seriesContext)
    {
        if (seriesContext is null || entry.VideoType != "tvshowepisode")
        {
            return;
        }

        entry.ShowId ??= seriesContext.ShowId;
        entry.ShowTitle ??= seriesContext.ShowTitle;
        entry.SeasonNumber ??= seriesContext.SeasonNumber;
        entry.EpisodeNumber ??= seriesContext.EpisodeNumber;
    }

    /// <summary>
    /// Überträgt den mitgegebenen Playlist-Bezug in den Manifesteintrag —
    /// nur, wenn Werte bekannt sind (vorhandene Werte eines Re-Enqueue
    /// werden nicht zurückgesetzt). Gilt für alle ladbaren Medientypen —
    /// auch Filme können playlist-gebunden eingeplant werden.
    /// </summary>
    /// <param name="entry">Der zu ergänzende Manifesteintrag.</param>
    /// <param name="playlistContext">Der Playlist-Bezug; <c>null</c> = keine Änderung.</param>
    private static void ApplyPlaylistContext(DownloadManifestEntry entry, DownloadPlaylistContext? playlistContext)
    {
        if (playlistContext is null)
        {
            return;
        }

        entry.PlaylistId ??= playlistContext.PlaylistId;
        entry.PlaylistEntryId ??= playlistContext.PlaylistEntryId;
        entry.PlaylistSortOrder ??= playlistContext.PlaylistSortOrder;
    }

    /// <summary>
    /// Bricht einen wartenden oder laufenden Download ab. Laufende
    /// Transfers enden im Zustand <see cref="DownloadState.Cancelled"/>
    /// (Teildatei wird entfernt); wartende Aufträge werden direkt als
    /// abgebrochen markiert. Abgebrochene Downloads werden nicht
    /// automatisch wiederaufgenommen.
    /// </summary>
    /// <param name="mediaType">Medientyp des Downloads.</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="cancellationToken">Bricht die Manifest-Operation ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Abbruch.</returns>
    public async Task CancelAsync(string mediaType, long mediaId, CancellationToken cancellationToken = default)
    {
        var type = MediaTypeNames.Normalize(mediaType);
        if (runningTransfers.TryGetValue(ManifestKey(type, mediaId), out var cts))
        {
            // Der Transfer meldet den Abbruch selbst als „cancelled"
            // (SetStateAsync-Eintrag) — kein zusätzlicher Zwischeneintrag.
            cts.Cancel();
            return;
        }

        var entry = await FindEntryAsync(type, mediaId);
        if (entry is not null && DownloadStateNames.FromPersisted(entry.State) == DownloadState.Queued)
        {
            await SetStateAsync(entry, DownloadState.Cancelled, null);
        }
    }

    /// <summary>
    /// Löscht einen Download vollständig: bricht einen laufenden Transfer
    /// ab, entfernt die lokale Datei und die Manifest-Zeile und meldet das
    /// Entfernen über <see cref="ItemRemoved"/>.
    /// </summary>
    /// <param name="mediaType">Medientyp des Downloads.</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="cancellationToken">Bricht die Manifest-Operation ab.</param>
    /// <returns><c>true</c>, wenn ein Manifest-Eintrag entfernt wurde.</returns>
    public async Task<bool> DeleteAsync(string mediaType, long mediaId, CancellationToken cancellationToken = default)
    {
        var type = MediaTypeNames.Normalize(mediaType);
        var key = ManifestKey(type, mediaId);
        if (runningTransfers.TryGetValue(key, out var cts))
        {
            cts.Cancel();
        }

        var entry = await FindEntryAsync(type, mediaId);
        TryDeleteFile(entry is null ? FilePathFor(type, mediaId) : DownloadManifestPaths.Resolve(layout, entry));
        if (entry is null)
        {
            return false;
        }

        await DeleteManifestEntryAsync(entry, cancellationToken);
        logger.LogInformation(
            "Download {MediaType}/{MediaId} ({Title}) entfernt.",
            type,
            mediaId,
            entry.Title);
        return true;
    }

    /// <summary>
    /// Verkürzt das Ablaufdatum eines abgeschlossenen Downloads auf
    /// <c>jetzt + <paramref name="delay"/></c> — Träger der Option
    /// „Downloads nach dem Abspielen automatisch löschen"; die eigentliche
    /// Entfernung übernimmt der nächste Bereinigungslauf. Nicht
    /// abgeschlossene/fehlende Einträge bleiben unberührt.
    /// </summary>
    /// <param name="mediaType">Medientyp des Mediums.</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="delay">Löschverzögerung ab Jetzt.</param>
    /// <param name="cancellationToken">Bricht die Operation ab.</param>
    /// <returns><c>true</c>, wenn das Ablaufdatum eines <c>completed</c>-Eintrags gesetzt wurde.</returns>
    public Task<bool> ExpireAfterAsync(
        string mediaType, long mediaId, TimeSpan delay, CancellationToken cancellationToken = default) =>
        UpdateCompletedExpiryAsync(
            mediaType, mediaId, _ => utcNow() + delay, cancellationToken);

    /// <summary>
    /// Gerüst der Ablaufdatum-Änderungen (<see cref="ExpireAfterAsync"/>,
    /// <see cref="ExtendExpiryAsync"/>): Eintrag suchen und die
    /// <c>completed</c>-Vorbedingung prüfen, das Ziel über
    /// <paramref name="computeTargetUtc"/> berechnen (<c>null</c> =
    /// keine Änderung), schreiben und die Änderung nach dem
    /// Gate-Release über <see cref="ItemChanged"/> melden.
    /// </summary>
    /// <param name="mediaType">Medientyp des Mediums (wird normalisiert).</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="computeTargetUtc">Zielberechnung aus dem gefundenen Eintrag; <c>null</c> = Eintrag bleibt unberührt.</param>
    /// <param name="cancellationToken">Bricht die Operation ab.</param>
    /// <returns><c>true</c>, wenn das Ablaufdatum eines <c>completed</c>-Eintrags geändert wurde.</returns>
    private async Task<bool> UpdateCompletedExpiryAsync(
        string mediaType,
        long mediaId,
        Func<DownloadManifestEntry, DateTime?> computeTargetUtc,
        CancellationToken cancellationToken)
    {
        var type = MediaTypeNames.Normalize(mediaType);
        DownloadListItem? changedItem = null;
        // Lesen, Vorbedingung und Schreiben unter dem Gate — ein
        // paralleler Schreiber darf nicht zwischen Prüfung und
        // Schreiben dazwischenfunken (Muster EnqueueAsync).
        await manifestGate.WaitAsync(cancellationToken);
        try
        {
            var entry = await FindEntryAsync(type, mediaId);
            if (entry is null || DownloadStateNames.FromPersisted(entry.State) != DownloadState.Completed)
            {
                return false;
            }

            var untilUtc = computeTargetUtc(entry);
            if (untilUtc is null)
            {
                return false;
            }

            entry.ExpiresAtUtc = untilUtc;
            entry.UpdatedAtUtc = utcNow();
            await cache.Connection.UpdateAsync(entry);
            changedItem = ToItem(entry);
        }
        finally
        {
            manifestGate.Release();
        }

        // Das Ereignis wird erst nach dem Gate-Release ausgelöst —
        // der kritische Abschnitt bleibt kurz und Handler, die
        // synchron auf den Dienst zugreifen, laufen nicht in das
        // noch gehaltene Gate (Muster UpdateManifestEntryAsync).
        RaiseItemChanged(changedItem!);
        return true;
    }

    /// <summary>
    /// Setzt das Ablaufdatum eines abgeschlossenen Downloads auf
    /// <c>jetzt + <see cref="PlaybackProtectionFactor"/> ×
    /// <paramref name="remaining"/></c> hoch — Wiedergabe-Schutz:
    /// während des Abspielens einer lokalen Datei darf der
    /// Bereinigungslauf den Download nicht entfernen. Bestehende
    /// spätere Ablaufdaten bleiben bestehen (nur Hochsetzung); nicht
    /// abgeschlossene/fehlende Einträge bleiben unberührt.
    /// </summary>
    /// <param name="mediaType">Medientyp des Mediums.</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="remaining">Verbleibende Wiedergabedauer — Basis der Hochsetzung (über die Zeitquelle).</param>
    /// <param name="cancellationToken">Bricht die Operation ab.</param>
    /// <returns><c>true</c>, wenn das Ablaufdatum eines <c>completed</c>-Eintrags hochgesetzt wurde.</returns>
    public Task<bool> ExtendExpiryAsync(
        string mediaType, long mediaId, TimeSpan remaining, CancellationToken cancellationToken = default) =>
        UpdateCompletedExpiryAsync(
            mediaType, mediaId,
            // Nur Hochsetzung: ein bestehendes späteres Ablaufdatum
            // bleibt bestehen (Selektor liefert null = keine Änderung).
            entry =>
            {
                var untilUtc = utcNow() + PlaybackProtectionFactor * remaining;
                return entry.ExpiresAtUtc is not null && entry.ExpiresAtUtc >= untilUtc
                    ? null
                    : untilUtc;
            },
            cancellationToken);

    /// <summary>
    /// Nimmt unterbrochene Downloads wieder auf: wartende und (z. B. nach
    /// einem App-Neustart) verwaist laufende Aufträge sowie nicht-Quota-
    /// Fehlschläge werden erneut eingeplant. Abgebrochene Downloads und
    /// Quota-Fehlschläge bleiben unberührt. Da der Server-Stream keine
    /// Bereichsanfragen unterstützt, beginnt der Transfer erneut ab
    /// Byte 0.
    /// </summary>
    /// <param name="settings">Aktuelle App-Einstellungen (Kontingent, WLAN-Bedingung).</param>
    /// <param name="cancellationToken">Bricht die Operation ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach der Wiederaufnahme-Prüfung.</returns>
    public async Task ResumeInterruptedAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        List<DownloadManifestEntry> toRequeue;
        try
        {
            var entries = await cache.Connection.Table<DownloadManifestEntry>().ToListAsync();
            toRequeue = entries
                .Where(e =>
                {
                    var state = DownloadStateNames.FromPersisted(e.State);
                    return state is DownloadState.Queued or DownloadState.Downloading
                        || (state == DownloadState.Failed && e.Error != ErrorQuotaExceeded);
                })
                .ToList();
        }
        catch (Exception ex) when (ex is SQLite.SQLiteException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Download-Manifest nicht lesbar.");
            return;
        }

        foreach (var entry in toRequeue)
        {
            if (DownloadStateNames.FromPersisted(entry.State) != DownloadState.Queued || entry.Error is not null)
            {
                entry.State = DownloadStateNames.ToPersisted(DownloadState.Queued);
                entry.Error = null;
                await UpdateManifestEntryAsync(entry, cancellationToken);
            }
        }

        if (toRequeue.Count > 0)
        {
            logger.LogInformation("{RequeuedCount} unterbrochene Downloads wieder eingeplant.", toRequeue.Count);
        }

        PumpQueue(settings);
    }

    /// <summary>
    /// Meldet den Abschluss eines systemseitig geführten Transfers (iOS-
    /// Hintergrund-Session), für den kein aktiver <c>TransferAsync</c>-Aufruf
    /// mehr existiert — z. B. nach App-Neustart durch das System.
    /// <paramref name="mediaKey"/> ist der Dateiname ohne Endung
    /// (<c>{typ}_{mediaId}</c>); <paramref name="tempPath"/> die vom System
    /// gelieferte fertige Datei, <paramref name="errorCode"/> der Fehlercode
    /// oder <c>null</c>.
    /// </summary>
    /// <param name="mediaKey">Der Medien-Key aus dem Dateinamen-Muster.</param>
    /// <param name="errorCode">Fehlercode des Transfers; <c>null</c> = Erfolg.</param>
    /// <param name="tempPath">Pfad der fertig geladenen Temp-Datei; <c>null</c> bei Fehler.</param>
    /// <param name="settings">Aktuelle App-Einstellungen (Aufbewahrungszeit für das Ablaufdatum der Fertigstellung).</param>
    /// <param name="cancellationToken">Bricht die Manifest-Operation ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach der Korrektur.</returns>
    public async Task CompleteExternalTransferAsync(
        string mediaKey, string? errorCode, string? tempPath, AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var parts = mediaKey.Split('|')[0].Split('_');
        if (parts.Length != 2 || !long.TryParse(parts[1], out var mediaId))
        {
            return;
        }

        var entry = await FindEntryAsync(parts[0], mediaId);
        if (entry is null || DownloadStateNames.FromPersisted(entry.State) != DownloadState.Downloading)
        {
            return;
        }

        if (errorCode is null && tempPath is not null)
        {
            try
            {
                entry.FilePath = DownloadManifestPaths.Resolve(layout, entry);
                File.Copy(tempPath, entry.FilePath, overwrite: true);
                entry.SizeBytes = new FileInfo(entry.FilePath).Length;
                entry.ExpiresAtUtc = utcNow() + TimeSpan.FromDays(settings.DownloadRetentionDays);
                await SetStateAsync(entry, DownloadState.Completed, null);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                await SetStateAsync(entry, DownloadState.Failed, ErrorIo, ex);
            }
        }
        else
        {
            await SetStateAsync(entry, DownloadState.Failed, errorCode ?? ErrorNetwork);
        }
    }

    /// <summary>
    /// Entfernt alle Downloads vollständig: bricht alle laufenden Transfers ab,
    /// löscht alle bekannten Dateien und leert das Manifest. Danach ist die
    /// Warteschlange leer — `PumpQueue` startet nichts Neues.
    /// </summary>
    /// <param name="cancellationToken">Bricht die Manifest-Operationen ab.</param>
    /// <returns>Die Anzahl entfernter Manifest-Einträge.</returns>
    public async Task<int> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var cts in runningTransfers.Values)
        {
            cts.Cancel();
        }

        List<DownloadManifestEntry> entries;
        try
        {
            entries = await cache.Connection.Table<DownloadManifestEntry>().ToListAsync();
        }
        catch (Exception ex) when (ex is SQLite.SQLiteException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Download-Manifest nicht lesbar — Gesamtlöschung übersprungen.");
            return 0;
        }

        foreach (var entry in entries)
        {
            TryDeleteFile(DownloadManifestPaths.Resolve(layout, entry));
        }

        await manifestGate.WaitAsync(cancellationToken);
        try
        {
            await cache.Connection.DeleteAllAsync<DownloadManifestEntry>();
        }
        finally
        {
            manifestGate.Release();
        }

        foreach (var entry in entries)
        {
            RaiseItemRemoved(entry.VideoType, entry.VideoId);
        }

        logger.LogInformation("{Count} Downloads vollständig entfernt.", entries.Count);
        return entries.Count;
    }

    /// <summary>
    /// Entfernt alle fehlgeschlagenen und abgebrochenen Downloads in einem
    /// Schritt: löscht deren Dateien und Manifest-Zeilen und meldet das
    /// Entfernen je Eintrag über <see cref="ItemRemoved"/>. Wartende,
    /// laufende und abgeschlossene Downloads bleiben unberührt — anders
    /// als <see cref="DeleteAllAsync"/> werden keine Transfers abgebrochen.
    /// </summary>
    /// <param name="cancellationToken">Bricht die Manifest-Operationen ab.</param>
    /// <returns>Die Anzahl entfernter Manifest-Einträge.</returns>
    public async Task<int> DeleteFailedAsync(CancellationToken cancellationToken = default)
    {
        List<DownloadManifestEntry> failed;
        try
        {
            var entries = await cache.Connection.Table<DownloadManifestEntry>().ToListAsync();
            failed = entries
                .Where(e => DownloadStateNames.FromPersisted(e.State)
                    is DownloadState.Failed or DownloadState.Cancelled)
                .ToList();
        }
        catch (Exception ex) when (ex is SQLite.SQLiteException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Download-Manifest nicht lesbar — Löschung der Fehlereinträge übersprungen.");
            return 0;
        }

        foreach (var entry in failed)
        {
            TryDeleteFile(entry.FilePath);
        }

        await manifestGate.WaitAsync(cancellationToken);
        try
        {
            foreach (var entry in failed)
            {
                await cache.Connection.DeleteAsync(entry);
            }
        }
        finally
        {
            manifestGate.Release();
        }

        foreach (var entry in failed)
        {
            RaiseItemRemoved(entry.VideoType, entry.VideoId);
        }

        logger.LogInformation("{Count} fehlgeschlagene/abgebrochene Downloads entfernt.", failed.Count);
        return failed.Count;
    }

    /// <summary>
    /// Entfernt erkannte Restdaten: Dateien im Video-Ablageort ohne
    /// Manifest-Eintrag (Dateien verwalteter Downloads bleiben unberührt).
    /// </summary>
    /// <param name="cancellationToken">Bricht die Operation ab.</param>
    /// <returns>Die freigegebenen Bytes.</returns>
    public async Task<long> PurgeOrphansAsync(CancellationToken cancellationToken = default)
    {
        List<DownloadManifestEntry> entries;
        string[] files;
        try
        {
            entries = await cache.Connection.Table<DownloadManifestEntry>().ToListAsync();
            files = Directory.Exists(layout.VideosDirectory)
                ? Directory.GetFiles(layout.VideosDirectory)
                : [];
        }
        catch (Exception ex) when (ex is SQLite.SQLiteException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Bereinigung der Restdaten nicht möglich.");
            return 0;
        }

        // Abgleich über den Dateinamen: der gespeicherte Verzeichnisanteil
        // kann nach einem Plattform-Container-Umzug veraltet sein.
        var manifestFileNames = new HashSet<string>(
            entries.Select(e => Path.GetFileName(e.FilePath)).Where(n => !string.IsNullOrWhiteSpace(n)),
            StringComparer.OrdinalIgnoreCase);

        var freed = 0L;
        var count = 0;
        foreach (var path in files)
        {
            if (manifestFileNames.Contains(Path.GetFileName(path)))
            {
                continue;
            }

            try
            {
                freed += new FileInfo(path).Length;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            TryDeleteFile(path);
            count++;
        }

        logger.LogInformation("{Count} Restdateien ohne Manifest-Eintrag entfernt ({Bytes} Bytes).", count, freed);
        return freed;
    }

    /// <summary>
    /// Gleicht den Video-Ablageort mit dem Download-Manifest ab:
    /// überholte Teildateien (Reste abgebrochener, fehlgeschlagener oder
    /// verwaist laufender Aufträge) werden entfernt; Dateien ohne
    /// Manifest-Eintrag werden als Restdaten erkannt und gezählt, aber
    /// nicht entfernt (Bereinigung ist eine Anwenderentscheidung); als
    /// „fertig" geführte Einträge ohne Datei werden als fehlgeschlagen
    /// (<see cref="ErrorMissingFile"/>) korrigiert und sind damit erneut
    /// einplanbar. Dateien von Einträgen im Zustand
    /// <see cref="DownloadState.Completed"/> und von gerade laufenden
    /// Transfers werden nie angefasst.
    /// <para>
    /// Mit übergebenen <paramref name="settings"/> läuft zusätzlich die
    /// Ablauf-Prüfung: abgeschlossene Einträge mit erreichtem (oder im
    /// Altbestand nicht gesetztem) Ablaufdatum werden entfernt — Datei,
    /// Manifest-Zeile, <see cref="ItemRemoved"/>; Einträge, deren Medium
    /// weiterhin Auto-Download-Kandidat ist (Schlüssel in
    /// <paramref name="autoDownloadCandidateKeys"/>), werden stattdessen
    /// auf <c>jetzt + Aufbewahrungszeit</c> verlängert und auf die
    /// Herkunft <c>auto</c> umgestellt.
    /// </para>
    /// </summary>
    /// <param name="settings">Aktuelle App-Einstellungen (Aufbewahrungszeit der Verlängerung); <c>null</c> = kein Ablauf-Pass.</param>
    /// <param name="autoDownloadCandidateKeys">Kandidatenmenge des Auto-Download-Vorlaufs im <see cref="ManifestKey"/>-Format; <c>null</c> = keine Kandidaten.</param>
    /// <param name="cancellationToken">Bricht die Manifest-Operationen ab.</param>
    /// <returns>Das Abgleichsergebnis mit den erkannten und entfernten Datenmengen.</returns>
    public async Task<DownloadReconcileResult> ReconcileAsync(
        AppSettings? settings = null,
        IReadOnlySet<string>? autoDownloadCandidateKeys = null,
        CancellationToken cancellationToken = default)
    {
        var entries = await ReadManifestEntriesAsync();
        if (entries is null)
        {
            return new DownloadReconcileResult();
        }

        // Abgleich über den Dateinamen: der gespeicherte Verzeichnisanteil
        // kann nach einem Plattform-Container-Umzug veraltet sein.
        var entryByFileName = entries
            .Where(e => !string.IsNullOrWhiteSpace(Path.GetFileName(e.FilePath)))
            .GroupBy(e => Path.GetFileName(e.FilePath), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var files = ReadVideoFileNames();
        if (files is null)
        {
            return new DownloadReconcileResult();
        }

        // Datei-Scan-Pass: Teildatei-Bereinigung, Restdaten-Erkennung und
        // Pfad-Heilung nach Container-Umzug.
        var (removedPartialBytes, removedPartialCount, orphanedBytes, orphanedFileCount) =
            await ScanFilesAsync(files, entryByFileName, cancellationToken);

        // Korrektur-Pass: „completed"-Einträge ohne lokale Datei werden
        // als fehlgeschlagen markiert (erneut einplanbar).
        var correctedCompleted = await CorrectCompletedWithoutFileAsync(entries, cancellationToken);

        // Ablauf-Pass: abgeschlossene Einträge mit erreichtem Ablaufdatum
        // (Altbestand ohne ExpiresAtUtc gilt als abgelaufen) werden
        // entfernt — es sei denn, das Medium ist weiterhin Auto-Download-
        // Kandidat, dann wird verlängert und die Herkunft auf „auto"
        // gestellt. Nur „completed" wird geprüft — laufende Transfers,
        // wartende und fehlerhafte Einträge bleiben unberührt.
        var removedExpiredCount = 0;
        var extendedExpiryCount = 0;
        if (settings is not null)
        {
            (removedExpiredCount, extendedExpiryCount) = await RunExpiryPassAsync(
                entries, settings, autoDownloadCandidateKeys, cancellationToken);
        }

        var result = new DownloadReconcileResult
        {
            RemovedPartialBytes = removedPartialBytes,
            RemovedPartialCount = removedPartialCount,
            OrphanedBytes = orphanedBytes,
            OrphanedFileCount = orphanedFileCount,
            CorrectedCompletedCount = correctedCompleted,
            RemovedExpiredCount = removedExpiredCount,
            ExtendedExpiryCount = extendedExpiryCount
        };
        LogReconcileResult(result);
        return result;
    }

    /// <summary>
    /// Liest alle Einträge des Download-Manifests für den Abgleich
    /// (<see cref="ReconcileAsync"/>); <c>null</c> bei nicht lesbarer
    /// Ablage — der Abgleich wird dann übersprungen.
    /// </summary>
    /// <returns>Die Manifest-Einträge; <c>null</c>, wenn das Manifest nicht lesbar ist.</returns>
    private async Task<List<DownloadManifestEntry>?> ReadManifestEntriesAsync()
    {
        try
        {
            return await cache.Connection.Table<DownloadManifestEntry>().ToListAsync();
        }
        catch (Exception ex) when (ex is SQLite.SQLiteException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Download-Manifest nicht lesbar — Abgleich übersprungen.");
            return null;
        }
    }

    /// <summary>
    /// Listet die Dateien des Video-Verzeichnisses für den Abgleich
    /// (<see cref="ReconcileAsync"/>); <c>null</c> bei nicht lesbarem
    /// Verzeichnis — der Abgleich wird dann übersprungen.
    /// </summary>
    /// <returns>Die Dateipfade; <c>null</c>, wenn das Verzeichnis nicht lesbar ist.</returns>
    private string[]? ReadVideoFileNames()
    {
        try
        {
            return Directory.Exists(layout.VideosDirectory)
                ? Directory.GetFiles(layout.VideosDirectory)
                : [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Video-Verzeichnis nicht lesbar — Abgleich übersprungen.");
            return null;
        }
    }

    /// <summary>
    /// Protokolliert das Abgleichsergebnis (<see cref="ReconcileAsync"/>)
    /// — nur bei tatsächlich erkannten oder entfernten Datenmengen.
    /// </summary>
    /// <param name="result">Das Abgleichsergebnis.</param>
    private void LogReconcileResult(DownloadReconcileResult result)
    {
        if (result.RemovedPartialCount > 0 || result.OrphanedFileCount > 0
            || result.CorrectedCompletedCount > 0 || result.RemovedExpiredCount > 0
            || result.ExtendedExpiryCount > 0)
        {
            logger.LogInformation(
                "Download-Abgleich: {RemovedCount} überholte Teildateien entfernt ({RemovedBytes} Bytes), "
                + "{OrphanedCount} Restdateien ohne Manifest-Eintrag erkannt ({OrphanedBytes} Bytes), "
                + "{CorrectedCount} fertige Einträge ohne Datei korrigiert, "
                + "{ExpiredCount} abgelaufene Downloads entfernt, "
                + "{ExtendedCount} Auto-Download-Kandidaten verlängert.",
                result.RemovedPartialCount,
                result.RemovedPartialBytes,
                result.OrphanedFileCount,
                result.OrphanedBytes,
                result.CorrectedCompletedCount,
                result.RemovedExpiredCount,
                result.ExtendedExpiryCount);
        }
    }

    /// <summary>
    /// Datei-Scan-Pass des Abgleichs (<see cref="ReconcileAsync"/>): jede
    /// Datei des Video-Verzeichnisses wird über den Dateinamen dem
    /// Manifest zugeordnet. Dateien ohne Manifest-Bezug sind verwaiste
    /// Restdaten — sie werden gezählt, aber nicht entfernt (Bereinigung
    /// ist eine Anwenderentscheidung). Einträge mit veraltetem
    /// Verzeichnisanteil (Container-Umzug) werden auf den tatsächlichen
    /// Ablageort geheilt. Überholte Teildateien — der Eintrag wartet,
    /// ist fehlgeschlagen/abgebrochen oder „läuft" ohne tatsächlichen
    /// Transfer — werden entfernt. Dateien von Einträgen im Zustand
    /// <see cref="DownloadState.Completed"/> und von gerade laufenden
    /// Transfers werden nie angefasst.
    /// </summary>
    /// <param name="files">Die Dateien des Video-Verzeichnisses.</param>
    /// <param name="entryByFileName">Manifest-Einträge nach Dateinamen.</param>
    /// <param name="cancellationToken">Bricht die Manifest-Operationen ab.</param>
    /// <returns>Die Mengen der entfernten Teildateien und der erkannten Restdaten (Anzahl und Bytes).</returns>
    private async Task<(long RemovedPartialBytes, int RemovedPartialCount, long OrphanedBytes, int OrphanedFileCount)>
        ScanFilesAsync(
            string[] files,
            IReadOnlyDictionary<string, DownloadManifestEntry> entryByFileName,
            CancellationToken cancellationToken)
    {
        var removedPartialBytes = 0L;
        var removedPartialCount = 0;
        var orphanedBytes = 0L;
        var orphanedFileCount = 0;
        foreach (var path in files)
        {
            long size;
            try
            {
                // Kann zwischen Enumeration und Zugriff verschwinden
                // (paralleler Transfer/Benutzer) — dann nichts zu zählen.
                size = new FileInfo(path).Length;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (!entryByFileName.TryGetValue(Path.GetFileName(path), out var entry))
            {
                // Kein Manifest-Bezug → verwaiste Restdaten: erfassen,
                // aber nicht entfernen (Anwender entscheidet).
                orphanedFileCount++;
                orphanedBytes += size;
                continue;
            }

            // Container-Umzug (iOS verschiebt den Datencontainer bei Updates):
            // der gespeicherte Verzeichnisanteil ist veraltet — auf den
            // tatsächlichen Ablageort heilen.
            if (!string.Equals(entry.FilePath, path, StringComparison.OrdinalIgnoreCase))
            {
                entry.FilePath = path;
                await UpdateManifestEntryAsync(entry, cancellationToken);
            }

            var state = DownloadStateNames.FromPersisted(entry.State);
            var isRunning = state == DownloadState.Downloading
                && runningTransfers.ContainsKey(ManifestKey(entry.VideoType, entry.VideoId));

            // Überholte Teildatei: Eintrag wartet, ist fehlgeschlagen/
            // abgebrochen oder „läuft" ohne tatsächlichen Transfer — die
            // Datei ist ein Rest (verwaltete Fehlerpfade löschen sie
            // bereits; Rückstände entstehen durch Prozessabbruch).
            if (state != DownloadState.Completed && !isRunning)
            {
                removedPartialCount++;
                removedPartialBytes += size;
                TryDeleteFile(path);
            }
        }

        return (removedPartialBytes, removedPartialCount, orphanedBytes, orphanedFileCount);
    }

    /// <summary>
    /// Korrektur-Pass des Abgleichs (<see cref="ReconcileAsync"/>): als
    /// „fertig" (<see cref="DownloadState.Completed"/>) geführte Einträge
    /// ohne lokale Datei werden als fehlgeschlagen
    /// (<see cref="ErrorMissingFile"/>) korrigiert und sind damit erneut
    /// einplanbar.
    /// </summary>
    /// <param name="entries">Die Manifest-Einträge des Abgleichs.</param>
    /// <param name="cancellationToken">Bricht die Manifest-Operationen ab.</param>
    /// <returns>Die Anzahl der korrigierten Einträge.</returns>
    private async Task<int> CorrectCompletedWithoutFileAsync(
        List<DownloadManifestEntry> entries,
        CancellationToken cancellationToken)
    {
        var correctedCompleted = 0;
        foreach (var entry in entries)
        {
            if (DownloadStateNames.FromPersisted(entry.State) == DownloadState.Completed
                && !fileExists(DownloadManifestPaths.Resolve(layout, entry)))
            {
                await SetStateAsync(entry, DownloadState.Failed, ErrorMissingFile);
                correctedCompleted++;
            }
        }

        return correctedCompleted;
    }

    /// <summary>
    /// Ablauf-Pass des Abgleichs (<see cref="ReconcileAsync"/>):
    /// abgeschlossene Einträge mit erreichtem (oder im Altbestand nicht
    /// gesetztem) Ablaufdatum werden entfernt — Datei, Manifest-Zeile,
    /// <see cref="ItemRemoved"/>; Einträge, deren Medium weiterhin
    /// Auto-Download-Kandidat ist (Schlüssel in
    /// <paramref name="autoDownloadCandidateKeys"/>), werden stattdessen
    /// auf <c>jetzt + Aufbewahrungszeit</c> verlängert und auf die
    /// Herkunft <c>auto</c> umgestellt. Nur „completed" wird geprüft —
    /// laufende Transfers, wartende und fehlerhafte Einträge bleiben
    /// unberührt.
    /// </summary>
    /// <param name="entries">Die Manifest-Einträge des Abgleichs.</param>
    /// <param name="settings">Aktuelle App-Einstellungen (Aufbewahrungszeit der Verlängerung).</param>
    /// <param name="autoDownloadCandidateKeys">Kandidatenmenge des Auto-Download-Vorlaufs im <see cref="ManifestKey"/>-Format; <c>null</c> = keine Kandidaten.</param>
    /// <param name="cancellationToken">Bricht die Manifest-Operationen ab.</param>
    /// <returns>Die Anzahl entfernter abgelaufener Einträge und die Anzahl verlängerter Auto-Download-Kandidaten.</returns>
    private async Task<(int RemovedExpiredCount, int ExtendedExpiryCount)> RunExpiryPassAsync(
        List<DownloadManifestEntry> entries,
        AppSettings settings,
        IReadOnlySet<string>? autoDownloadCandidateKeys,
        CancellationToken cancellationToken)
    {
        var removedExpiredCount = 0;
        var extendedExpiryCount = 0;
        var now = utcNow();
        var retention = TimeSpan.FromDays(settings.DownloadRetentionDays);
        foreach (var entry in entries)
        {
            if (DownloadStateNames.FromPersisted(entry.State) != DownloadState.Completed
                || runningTransfers.ContainsKey(ManifestKey(entry.VideoType, entry.VideoId))
                || (entry.ExpiresAtUtc is not null && entry.ExpiresAtUtc > now))
            {
                continue;
            }

            if (autoDownloadCandidateKeys is not null
                && autoDownloadCandidateKeys.Contains(ManifestKey(entry.VideoType, entry.VideoId)))
            {
                entry.ExpiresAtUtc = now + retention;
                entry.Origin = DownloadOriginNames.ToPersisted(DownloadOrigin.Auto);
                await UpdateManifestEntryAsync(entry, cancellationToken);
                extendedExpiryCount++;
                continue;
            }

            TryDeleteFile(DownloadManifestPaths.Resolve(layout, entry));
            await DeleteManifestEntryAsync(entry, cancellationToken);
            removedExpiredCount++;
        }

        return (removedExpiredCount, extendedExpiryCount);
    }

    private async Task PumpQueueAsync(AppSettings settings)
    {
        try
        {
            await pumpGate.WaitAsync();
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        try
        {
            // WLAN-Bedingung: wartende Aufträge bleiben eingeplant und
            // werden bei der nächsten Pump-Gelegenheit (Netzwerkwechsel)
            // gestartet.
            if (settings.WifiOnlyDownloads && !networkMonitor.IsWifi)
            {
                return;
            }

            while (runningTransfers.Count < MaxParallelDownloads)
            {
                var entry = await NextQueuedEntryAsync();
                if (entry is null)
                {
                    break;
                }

                var key = ManifestKey(entry.VideoType, entry.VideoId);
                var cts = new CancellationTokenSource();
                if (!runningTransfers.TryAdd(key, cts))
                {
                    cts.Dispose();
                    continue;
                }

                if (GetUsedBytes() >= QuotaBytesFor(settings))
                {
                    runningTransfers.TryRemove(key, out _);
                    cts.Dispose();
                    await SetStateAsync(entry, DownloadState.Failed, ErrorQuotaExceeded);
                    continue;
                }

                var usedAtStart = GetUsedBytes();
                await SetStateAsync(entry, DownloadState.Downloading, null);
                _ = TransferAsync(entry, settings, cts, usedAtStart);
            }
        }
        finally
        {
            pumpGate.Release();
        }
    }

    private void PumpQueue(AppSettings settings)
    {
        _ = PumpQueueAsync(settings);
    }

    private async Task<DownloadManifestEntry?> NextQueuedEntryAsync()
    {
        var entries = await cache.Connection.Table<DownloadManifestEntry>().ToListAsync();
        return entries
            .Where(e => DownloadStateNames.FromPersisted(e.State) == DownloadState.Queued
                && !runningTransfers.ContainsKey(ManifestKey(e.VideoType, e.VideoId)))
            .OrderBy(e => e.Id)
            .FirstOrDefault();
    }

    private async Task TransferAsync(
        DownloadManifestEntry entry,
        AppSettings settings,
        CancellationTokenSource cts,
        long usedAtStart)
    {
        var key = ManifestKey(entry.VideoType, entry.VideoId);
        var quotaBytes = QuotaBytesFor(settings);
        try
        {
            var url = await BuildStreamUrlAsync(entry.VideoType, entry.VideoId);
            if (url is null)
            {
                await SetStateIfPresentAsync(entry, DownloadState.Failed, ErrorNoConnection);
                return;
            }

            entry.TotalBytes = 0;
            entry.DownloadedBytes = 0;
            await UpdateManifestEntryAsync(entry, cts.Token);

            Directory.CreateDirectory(layout.VideosDirectory);
            // Wieder eingeplante Einträge können einen veralteten
            // Verzeichnisanteil tragen — Schreibziel im aktuellen
            // Ablageort auflösen und den Eintrag damit heilen.
            entry.FilePath = DownloadManifestPaths.Resolve(layout, entry);
            var quotaExceeded = false;
            var scannedUsed = usedAtStart;
            var downloadedAtScan = 0L;
            var progressTicks = 0;
            var progress = new Progress<(long Downloaded, long Total)>(p =>
            {
                entry.DownloadedBytes = p.Downloaded;
                if (p.Total > 0)
                {
                    entry.TotalBytes = p.Total;
                }

                // Laufende Quota-Prüfung: periodische Neumessung hält das
                // Kontingent auch über Transfer-Grenzen hinweg ein
                // (parallele Transfers schreiben in dasselbe Verzeichnis).
                if (++progressTicks >= QuotaScanIntervalChunks)
                {
                    scannedUsed = GetUsedBytes();
                    downloadedAtScan = p.Downloaded;
                    progressTicks = 0;
                }

                if (scannedUsed + (p.Downloaded - downloadedAtScan) > quotaBytes)
                {
                    quotaExceeded = true;
                    cts.Cancel();
                    return;
                }

                _ = ReportProgressThrottledAsync(key, entry);
            });

            var activeBackend = backend ?? new HttpDownloadBackend(httpClientFactory);
            DownloadBackendResult result;
            try
            {
                result = await activeBackend.TransferAsync(
                    url.Value.Url, url.Value.Token, entry.FilePath,
                    settings.WifiOnlyDownloads, progress, cts.Token);
            }
            catch (OperationCanceledException) when (quotaExceeded)
            {
                result = new DownloadBackendResult(0, 0, ErrorQuotaExceeded);
            }

            if (quotaExceeded || result.ErrorCode == ErrorQuotaExceeded)
            {
                TryDeleteFile(entry.FilePath);
                await SetStateIfPresentAsync(entry, DownloadState.Failed, ErrorQuotaExceeded);
                return;
            }

            if (result.ErrorCode is not null)
            {
                TryDeleteFile(entry.FilePath);
                await SetStateIfPresentAsync(entry, DownloadState.Failed, result.ErrorCode);
                return;
            }

            entry.DownloadedBytes = result.BytesWritten;
            entry.SizeBytes = result.BytesWritten;
            if (result.TotalBytes > 0)
            {
                entry.TotalBytes = result.TotalBytes;
            }

            // Ablaufdatum der Fertigstellung: die Aufbewahrungsfrist
            // beginnt erst mit „completed".
            entry.ExpiresAtUtc = utcNow() + TimeSpan.FromDays(settings.DownloadRetentionDays);
            await SetStateIfPresentAsync(entry, DownloadState.Completed, null);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            TryDeleteFile(entry.FilePath);
            await SetStateIfPresentAsync(entry, DownloadState.Cancelled, null);
        }
        catch (OperationCanceledException ex)
        {
            // Zeitüberschreitung/abgebrochene Übertragung ohne Benutzerabbruch.
            TryDeleteFile(entry.FilePath);
            await SetStateIfPresentAsync(entry, DownloadState.Failed, ErrorNetwork, ex);
        }
        catch (HttpRequestException ex)
        {
            TryDeleteFile(entry.FilePath);
            var code = ex.StatusCode is { } statusCode
                ? $"{ErrorHttpPrefix}{(int)statusCode}"
                : ErrorNetwork;
            await SetStateIfPresentAsync(entry, DownloadState.Failed, code, ex);
        }
        catch (IOException ex)
        {
            TryDeleteFile(entry.FilePath);
            await SetStateIfPresentAsync(entry, DownloadState.Failed, ErrorIo, ex);
        }
        catch (Exception ex)
        {
            TryDeleteFile(entry.FilePath);
            await SetStateIfPresentAsync(entry, DownloadState.Failed, ErrorNetwork, ex);
        }
        finally
        {
            lastProgressEmit.TryRemove(key, out _);
            if (runningTransfers.TryRemove(key, out var removed))
            {
                removed.Dispose();
            }

            PumpQueue(settings);
        }
    }

    private async Task ReportProgressThrottledAsync(string key, DownloadManifestEntry entry)
    {
        var now = DateTime.UtcNow;
        if (lastProgressEmit.TryGetValue(key, out var last) && now - last < ProgressInterval)
        {
            return;
        }

        lastProgressEmit[key] = now;
        await UpdateManifestEntryAsync(entry, CancellationToken.None);
    }

    /// <summary>
    /// Persistiert einen geänderten Manifest-Eintrag unter dem
    /// <see cref="manifestGate"/> (Zeitstempel über die Zeitquelle) und
    /// meldet die Änderung über <see cref="ItemChanged"/>.
    /// </summary>
    /// <param name="entry">Der geänderte Manifest-Eintrag.</param>
    /// <param name="cancellationToken">Bricht die Manifest-Operation ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Schreiben.</returns>
    private async Task UpdateManifestEntryAsync(
        DownloadManifestEntry entry, CancellationToken cancellationToken)
    {
        entry.UpdatedAtUtc = utcNow();
        await manifestGate.WaitAsync(cancellationToken);
        try
        {
            await cache.Connection.UpdateAsync(entry);
        }
        finally
        {
            manifestGate.Release();
        }

        RaiseItemChanged(ToItem(entry));
    }

    /// <summary>
    /// Entfernt einen Manifest-Eintrag unter dem <see cref="manifestGate"/>
    /// und meldet das Entfernen über <see cref="ItemRemoved"/>.
    /// </summary>
    /// <param name="entry">Der zu entfernende Manifest-Eintrag.</param>
    /// <param name="cancellationToken">Bricht die Manifest-Operation ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Schreiben.</returns>
    private async Task DeleteManifestEntryAsync(
        DownloadManifestEntry entry, CancellationToken cancellationToken)
    {
        await manifestGate.WaitAsync(cancellationToken);
        try
        {
            await cache.Connection.DeleteAsync(entry);
        }
        finally
        {
            manifestGate.Release();
        }

        RaiseItemRemoved(entry.VideoType, entry.VideoId);
    }

    private async Task SetStateAsync(DownloadManifestEntry entry, DownloadState state, string? error, Exception? exception = null)
    {
        entry.State = DownloadStateNames.ToPersisted(state);
        entry.Error = error;
        entry.UpdatedAtUtc = utcNow();
        await manifestGate.WaitAsync();
        try
        {
            await cache.Connection.UpdateAsync(entry);
        }
        finally
        {
            manifestGate.Release();
        }

        if (state == DownloadState.Failed)
        {
            logger.LogWarning(
                exception,
                "Download {VideoType}/{VideoId} ({Title}): {State} ({Error}).",
                entry.VideoType,
                entry.VideoId,
                entry.Title,
                state,
                error);
        }
        else
        {
            logger.LogInformation(
                "Download {VideoType}/{VideoId} ({Title}): {State}.",
                entry.VideoType,
                entry.VideoId,
                entry.Title,
                state);
        }

        RaiseItemChanged(ToItem(entry));
    }

    /// <summary>
    /// Zustandswechsel nur bei noch vorhandenem Manifest-Eintrag — ein
    /// zwischenzeitlich gelöschter Download darf keinen Listeneintrag
    /// mehr erzeugen (Löschen während eines laufenden Transfers).
    /// </summary>
    /// <param name="entry">Der zuletzt bekannte Manifest-Eintrag.</param>
    /// <param name="state">Der zu setzende Zustand.</param>
    /// <param name="error">Der Fehlercode; <c>null</c> = kein Fehler.</param>
    /// <param name="exception">Die auslösende Ausnahme; wird im Fehlerfall mitprotokolliert.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Schreiben.</returns>
    private async Task SetStateIfPresentAsync(DownloadManifestEntry entry, DownloadState state, string? error, Exception? exception = null)
    {
        if (await FindEntryAsync(entry.VideoType, entry.VideoId) is null)
        {
            return;
        }

        await SetStateAsync(entry, state, error, exception);
    }

    private async Task<DownloadManifestEntry?> FindEntryAsync(string type, long mediaId)
    {
        var id = (int)mediaId;
        var candidates = await cache.Connection.Table<DownloadManifestEntry>()
            .Where(e => e.VideoId == id)
            .ToListAsync();
        return candidates.FirstOrDefault(e =>
            MediaTypeNames.Normalize(e.VideoType) == type);
    }

    private async Task<(string Url, string? Token)?> BuildStreamUrlAsync(string type, long mediaId)
    {
        var baseAddress = authSession.ClientBaseAddress?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseAddress))
        {
            return null;
        }

        var token = authSession.Client?.CurrentToken ?? await secrets.GetAuthTokenAsync();
        var url = $"{baseAddress}/api/items/{type}/{mediaId}/stream";
        if (!string.IsNullOrWhiteSpace(token))
        {
            url += $"?access_token={Uri.EscapeDataString(token)}";
        }

        return (url, string.IsNullOrWhiteSpace(token) ? null : token);
    }

    private long QuotaBytesFor(AppSettings settings) =>
        quotaBytesOverride ?? settings.DownloadQuotaGb * BytesPerGigabyte;

    private string FilePathFor(string type, long mediaId) =>
        Path.Combine(layout.VideosDirectory, $"{type}_{mediaId}.mp4");

    /// <summary>
    /// Manifest-Key eines Mediums (<c>{typ}:{mediaId}</c>, normalisierter
    /// Medientyp) — gemeinsame Konvention der Transfer-Verwaltung und der
    /// Auto-Download-Kandidatenmenge des Bereinigungslaufs.
    /// </summary>
    /// <param name="mediaType">Der Medientyp (wird normalisiert).</param>
    /// <param name="mediaId">Die Medien-Id.</param>
    /// <returns>Der Manifest-Key.</returns>
    internal static string ManifestKey(string mediaType, long mediaId) =>
        $"{MediaTypeNames.Normalize(mediaType)}:{mediaId}";

    private void TryDeleteFile(string? path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Reste werden bei der nächsten Quota-Berechnung mitgezählt —
            // kein kritischer Fehler.
            logger.LogWarning(ex, "Datei löschen fehlgeschlagen.");
        }
    }

    private static DownloadListItem ToItem(DownloadManifestEntry entry)
    {
        var type = MediaTypeNames.Normalize(entry.VideoType);
        return new DownloadListItem
        {
            EntryId = entry.Id,
            MediaType = type,
            MediaId = entry.VideoId,
            PictureId = entry.PictureId,
            Title = string.IsNullOrWhiteSpace(entry.Title)
                ? $"Medium {entry.VideoId}"
                : entry.Title,
            State = DownloadStateNames.FromPersisted(entry.State),
            Origin = DownloadOriginNames.FromPersisted(entry.Origin),
            ExpiresAtUtc = entry.ExpiresAtUtc,
            DownloadedBytes = entry.DownloadedBytes,
            TotalBytes = entry.TotalBytes,
            SizeBytes = entry.SizeBytes,
            Error = string.IsNullOrWhiteSpace(entry.Error) ? null : entry.Error
        };
    }

    private void RaiseItemChanged(DownloadListItem item)
    {
        try
        {
            ItemChanged?.Invoke(this, new DownloadItemEventArgs { Item = item });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ItemChanged-Handler fehlgeschlagen.");
        }
    }

    private void RaiseItemRemoved(string type, long mediaId)
    {
        try
        {
            ItemRemoved?.Invoke(this, new DownloadRemovedEventArgs { MediaType = type, MediaId = mediaId });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ItemRemoved-Handler fehlgeschlagen.");
        }
    }

    private static HttpClient CreateHttpClient()
    {
        // Downloads großer Dateien dürfen nicht am Request-Timeout
        // scheitern — die Übertragung läuft ohne Zeitgrenze.
        return new HttpClient { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
    }
}
