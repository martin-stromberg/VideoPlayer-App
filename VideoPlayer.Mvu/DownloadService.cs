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
        : this(authSession, secrets, cache, layout, networkMonitor, null, null, null, logger, backend)
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
        IDownloadBackend? backend = null)
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
    /// <param name="mediaType">Medientyp (wird normalisiert; nur <c>movie</c>/<c>tvshowepisode</c> sind ladbar).</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="title">Anzeigetitel des Downloads; <c>null</c> = Fallback.</param>
    /// <param name="settings">Aktuelle App-Einstellungen (Kontingent, WLAN-Bedingung).</param>
    /// <param name="seriesContext">Optionaler Serienkontext eines Episoden-Downloads (Grundlage des Offline-„Weiterschauen").</param>
    /// <param name="playlistContext">Optionaler Playlist-Bezug des Downloads (Herkunft und Reihenfolge playlist-gebundener Einplanungen).</param>
    /// <param name="pictureId">Bild-Id des Vorschaubilds (<c>api/pictures/{id}</c> — Episoden-Still bzw. Film-Poster); <c>null</c> = kein Bild bekannt.</param>
    /// <param name="cancellationToken">Bricht die Manifest-Operation ab (nicht den Transfer).</param>
    /// <returns>Das Einreihe-Ergebnis mit Snapshot und Hinweistext.</returns>
    public async Task<DownloadEnqueueResult> EnqueueAsync(
        string mediaType,
        long mediaId,
        string? title,
        AppSettings settings,
        DownloadSeriesContext? seriesContext = null,
        DownloadPlaylistContext? playlistContext = null,
        long? pictureId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var type = MediaTypeNames.Normalize(mediaType);

        DownloadEnqueueResult Logged(DownloadEnqueueResult enqueueResult)
        {
            // Fehler-Notices (ungültiges Medium, Kontingent erschöpft)
            // sind überholbare Einzelfehler → Warning; nur die
            // tatsächliche Einplanung ist ein Verlaufsereignis
            // (Information). Dedup-Hinweise ohne Zustandsänderung
            // (AlreadyRunning/AlreadyQueued/AlreadyPresent) bleiben
            // Debug — Auto-Download-Läufe prüfen pro Kandidat und
            // würden sonst pro Eintrag der Liste einen Eintrag ohne
            // Vorgang erzeugen.
            if (enqueueResult.IsError)
            {
                logger.LogWarning(
                    "Download-Auftrag {MediaType}/{MediaId} ({Title}): {Notice}",
                    type,
                    mediaId,
                    title,
                    enqueueResult.Notice);
            }
            else if (enqueueResult.Notice == NoticeEnqueued)
            {
                logger.LogInformation(
                    "Download-Auftrag {MediaType}/{MediaId} ({Title}): {Notice}",
                    type,
                    mediaId,
                    title,
                    enqueueResult.Notice);
            }
            else
            {
                logger.LogDebug(
                    "Download-Auftrag {MediaType}/{MediaId} ({Title}): {Notice}",
                    type,
                    mediaId,
                    title,
                    enqueueResult.Notice);
            }

            return enqueueResult;
        }

        if (mediaId <= 0 || type is not ("movie" or "tvshowepisode"))
        {
            return Logged(new DownloadEnqueueResult
            {
                Item = null,
                Notice = NoticeInvalidMedia,
                IsError = true
            });
        }

        DownloadManifestEntry entry;
        await manifestGate.WaitAsync(cancellationToken);
        try
        {
            var existing = await FindEntryAsync(type, mediaId);
            if (existing is not null)
            {
                var state = DownloadStateNames.FromPersisted(existing.State);
                if (state == DownloadState.Downloading && runningTransfers.ContainsKey(Key(type, mediaId)))
                {
                    return Logged(new DownloadEnqueueResult { Item = ToItem(existing), Notice = NoticeAlreadyRunning });
                }
                if (state == DownloadState.Queued)
                {
                    return Logged(new DownloadEnqueueResult { Item = ToItem(existing), Notice = NoticeAlreadyQueued });
                }
                if (state == DownloadState.Completed && fileExists(DownloadManifestPaths.Resolve(layout, existing)))
                {
                    return Logged(new DownloadEnqueueResult { Item = ToItem(existing), Notice = NoticeAlreadyPresent });
                }

                // Fehlgeschlagen/abgebrochen/verwaist (completed ohne Datei,
                // downloading ohne laufenden Transfer) → erneut einreihen.
                existing.State = DownloadStateNames.ToPersisted(DownloadState.Queued);
                existing.Error = null;
                existing.DownloadedBytes = 0;
                existing.TotalBytes = 0;
                if (!string.IsNullOrWhiteSpace(title))
                {
                    existing.Title = title;
                }
                ApplySeriesContext(existing, seriesContext);
                ApplyPlaylistContext(existing, playlistContext);
                existing.PictureId ??= pictureId;
                existing.UpdatedAtUtc = DateTime.UtcNow;
                await cache.Connection.UpdateAsync(existing);
                entry = existing;
            }
            else
            {
                entry = new DownloadManifestEntry
                {
                    VideoId = (int)mediaId,
                    VideoType = type,
                    Title = title ?? string.Empty,
                    FilePath = FilePathFor(type, mediaId),
                    State = DownloadStateNames.ToPersisted(DownloadState.Queued),
                    UpdatedAtUtc = DateTime.UtcNow
                };
                ApplySeriesContext(entry, seriesContext);
                ApplyPlaylistContext(entry, playlistContext);
                entry.PictureId ??= pictureId;
                await cache.Connection.InsertAsync(entry);
            }
        }
        finally
        {
            manifestGate.Release();
        }

        // Vorab-Quota-Prüfung: ist das Kontingent bereits belegt, wird der
        // Auftrag sofort als fehlgeschlagen geführt statt zu warten.
        if (GetUsedBytes() >= QuotaBytesFor(settings))
        {
            // Der Zustandswechsel-Eintrag von SetStateAsync (Failed mit
            // quota_exceeded) trägt das Ereignis bereits als Warnung —
            // kein zweiter Warn-Eintrag mit dem Hinweistext für
            // denselben Vorgang.
            await SetStateAsync(entry, DownloadState.Failed, ErrorQuotaExceeded);
            return new DownloadEnqueueResult
            {
                Item = ToItem(entry),
                Notice = NoticeQuotaExceeded,
                IsError = true
            };
        }

        RaiseItemChanged(ToItem(entry));
        PumpQueue(settings);
        return Logged(new DownloadEnqueueResult { Item = ToItem(entry), Notice = NoticeEnqueued });
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
        if (runningTransfers.TryGetValue(Key(type, mediaId), out var cts))
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
        var key = Key(type, mediaId);
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

        await manifestGate.WaitAsync(cancellationToken);
        try
        {
            await cache.Connection.DeleteAsync(entry);
        }
        finally
        {
            manifestGate.Release();
        }

        RaiseItemRemoved(type, mediaId);
        logger.LogInformation(
            "Download {MediaType}/{MediaId} ({Title}) entfernt.",
            type,
            mediaId,
            entry.Title);
        return true;
    }

    /// <summary>
    /// Löscht den Download eines Mediums nur, wenn er abgeschlossen ist
    /// (automatisches Entfernen gesehener Episoden) — laufende oder
    /// wartende Aufträge bleiben unberührt.
    /// </summary>
    /// <param name="mediaType">Medientyp des Mediums.</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="cancellationToken">Bricht die Operation ab.</param>
    /// <returns><c>true</c>, wenn ein abgeschlossener Download entfernt wurde.</returns>
    public async Task<bool> DeleteIfCompletedAsync(string mediaType, long mediaId, CancellationToken cancellationToken = default)
    {
        var type = MediaTypeNames.Normalize(mediaType);
        var entry = await FindEntryAsync(type, mediaId);
        if (entry is null || DownloadStateNames.FromPersisted(entry.State) != DownloadState.Completed)
        {
            return false;
        }

        return await DeleteAsync(type, mediaId, cancellationToken);
    }

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
                entry.UpdatedAtUtc = DateTime.UtcNow;
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
    /// <param name="cancellationToken">Bricht die Manifest-Operation ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach der Korrektur.</returns>
    public async Task CompleteExternalTransferAsync(
        string mediaKey, string? errorCode, string? tempPath, CancellationToken cancellationToken = default)
    {
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
    /// </summary>
    /// <param name="cancellationToken">Bricht die Manifest-Operationen ab.</param>
    /// <returns>Das Abgleichsergebnis mit den erkannten und entfernten Datenmengen.</returns>
    public async Task<DownloadReconcileResult> ReconcileAsync(CancellationToken cancellationToken = default)
    {
        var removedPartialBytes = 0L;
        var removedPartialCount = 0;
        var orphanedBytes = 0L;
        var orphanedFileCount = 0;
        var correctedCompleted = 0;

        List<DownloadManifestEntry> entries;
        try
        {
            entries = await cache.Connection.Table<DownloadManifestEntry>().ToListAsync();
        }
        catch (Exception ex) when (ex is SQLite.SQLiteException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Download-Manifest nicht lesbar — Abgleich übersprungen.");
            return new DownloadReconcileResult();
        }

        // Abgleich über den Dateinamen: der gespeicherte Verzeichnisanteil
        // kann nach einem Plattform-Container-Umzug veraltet sein.
        var entryByFileName = entries
            .Where(e => !string.IsNullOrWhiteSpace(Path.GetFileName(e.FilePath)))
            .GroupBy(e => Path.GetFileName(e.FilePath), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        string[] files;
        try
        {
            files = Directory.Exists(layout.VideosDirectory)
                ? Directory.GetFiles(layout.VideosDirectory)
                : [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Video-Verzeichnis nicht lesbar — Abgleich übersprungen.");
            return new DownloadReconcileResult();
        }

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
                entry.UpdatedAtUtc = DateTime.UtcNow;
                await manifestGate.WaitAsync(cancellationToken);
                try
                {
                    await cache.Connection.UpdateAsync(entry);
                }
                finally
                {
                    manifestGate.Release();
                }
            }

            var state = DownloadStateNames.FromPersisted(entry.State);
            var isRunning = state == DownloadState.Downloading
                && runningTransfers.ContainsKey(Key(entry.VideoType, entry.VideoId));

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

        foreach (var entry in entries)
        {
            if (DownloadStateNames.FromPersisted(entry.State) == DownloadState.Completed
                && !fileExists(DownloadManifestPaths.Resolve(layout, entry)))
            {
                await SetStateAsync(entry, DownloadState.Failed, ErrorMissingFile);
                correctedCompleted++;
            }
        }

        if (removedPartialCount > 0 || orphanedFileCount > 0 || correctedCompleted > 0)
        {
            logger.LogInformation(
                "Download-Abgleich: {RemovedCount} überholte Teildateien entfernt ({RemovedBytes} Bytes), "
                + "{OrphanedCount} Restdateien ohne Manifest-Eintrag erkannt ({OrphanedBytes} Bytes), "
                + "{CorrectedCount} fertige Einträge ohne Datei korrigiert.",
                removedPartialCount,
                removedPartialBytes,
                orphanedFileCount,
                orphanedBytes,
                correctedCompleted);
        }

        return new DownloadReconcileResult
        {
            RemovedPartialBytes = removedPartialBytes,
            RemovedPartialCount = removedPartialCount,
            OrphanedBytes = orphanedBytes,
            OrphanedFileCount = orphanedFileCount,
            CorrectedCompletedCount = correctedCompleted
        };
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

                var key = Key(entry.VideoType, entry.VideoId);
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
                && !runningTransfers.ContainsKey(Key(e.VideoType, e.VideoId)))
            .OrderBy(e => e.Id)
            .FirstOrDefault();
    }

    private async Task TransferAsync(
        DownloadManifestEntry entry,
        AppSettings settings,
        CancellationTokenSource cts,
        long usedAtStart)
    {
        var key = Key(entry.VideoType, entry.VideoId);
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
            await manifestGate.WaitAsync(cts.Token);
            try
            {
                await cache.Connection.UpdateAsync(entry);
            }
            finally
            {
                manifestGate.Release();
            }
            RaiseItemChanged(ToItem(entry));

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
        await manifestGate.WaitAsync();
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

    private async Task SetStateAsync(DownloadManifestEntry entry, DownloadState state, string? error, Exception? exception = null)
    {
        entry.State = DownloadStateNames.ToPersisted(state);
        entry.Error = error;
        entry.UpdatedAtUtc = DateTime.UtcNow;
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

    private static string Key(string type, long mediaId) => $"{type}:{mediaId}";

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
