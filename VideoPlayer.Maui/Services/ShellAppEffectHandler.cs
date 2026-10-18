using System.Net;
using Microsoft.Extensions.Logging;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Discovery;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// Führt MVU-Effekte aus: Navigation über die Shell, Server-Erreichbarkeits-
/// prüfung über <see cref="IServerHealthProbe"/>, Ablage-Initialisierung und
/// Einstellungs-Persistenz über <see cref="VideoPlayer.Storage"/>, die
/// Serververwaltung (Laden, Discovery, Kandidaten-Prüfung, Speichern), das
/// Geräte-Pairing über <see cref="DevicePairingClient"/>/<see cref="DeviceTokenService"/>
/// sowie den Anmelde-Workflow über <see cref="AuthSessionService"/>.
/// </summary>
public sealed class ShellAppEffectHandler : IAppEffectHandler
{
    private readonly IServiceProvider services;
    private readonly IServerHealthProbe healthProbe;
    private readonly AuthSessionService authSession;
    private readonly DevicePairingClient pairing;
    private readonly DeviceTokenService deviceTokens;
    private readonly AppSecretsService secrets;
    private readonly INetworkStatusMonitor networkMonitor;
    private readonly ServerEventsService serverEvents;
    private readonly DownloadService downloads;
    private readonly AutoDownloadService autoDownloads;
    private readonly AppLogService appLog;
    private readonly ILogger<ShellAppEffectHandler> logger;

    /// <summary>Empfängeradresse des Protokollversands per Mail.</summary>
    private const string LogProtocolRecipient = "mstromberg84+heimkino@gmail.com";

    /// <summary>Initialisiert eine neue Instanz der <see cref="ShellAppEffectHandler"/>-Klasse.</summary>
    /// <param name="services">Dienstanbieter zum Auflösen der am Effekt beteiligten Dienste (inkl. <see cref="MvuHost"/>).</param>
    /// <param name="healthProbe">Erreichbarkeitsprüfung für den Server.</param>
    /// <param name="authSession">Sitzungsdienst für den Anmelde-Workflow.</param>
    /// <param name="pairing">Öffentlicher Pairing-Endpunkt-Client (ohne Gate-/Bearer-Header).</param>
    /// <param name="deviceTokens">Verwaltung der gepaarten Geräte-Tokens (Provider, Widerruf, Gate-Probe).</param>
    /// <param name="secrets">Sichere Ablage (Geräte-Tokens je Server).</param>
    /// <param name="networkMonitor">Netzwerkstatus-Erkennung; meldet Wechsel in den MVU-Zyklus.</param>
    /// <param name="serverEvents">Server-Ereignisverbindung; meldet Ereignisse und Trennung in den MVU-Zyklus.</param>
    /// <param name="downloads">Download-Verwaltung; meldet Zustands-/Fortschrittsänderungen in den MVU-Zyklus.</param>
    /// <param name="autoDownloads">Automatischer Vorlauf der Weiterschauen-Liste.</param>
    /// <param name="appLog">Datei-Protokoll-Dienst (Schaltzustand und Versand-Dateien).</param>
    /// <param name="logger">Logger des Handlers — Diagnosemeldungen laufen ins Programmprotokoll.</param>
    public ShellAppEffectHandler(
        IServiceProvider services,
        IServerHealthProbe healthProbe,
        AuthSessionService authSession,
        DevicePairingClient pairing,
        DeviceTokenService deviceTokens,
        AppSecretsService secrets,
        INetworkStatusMonitor networkMonitor,
        ServerEventsService serverEvents,
        DownloadService downloads,
        AutoDownloadService autoDownloads,
        AppLogService appLog,
        ILogger<ShellAppEffectHandler> logger)
    {
        this.services = services;
        this.healthProbe = healthProbe;
        this.authSession = authSession;
        this.pairing = pairing;
        this.deviceTokens = deviceTokens;
        this.secrets = secrets;
        this.networkMonitor = networkMonitor;
        this.serverEvents = serverEvents;
        this.downloads = downloads;
        this.autoDownloads = autoDownloads;
        this.appLog = appLog;
        this.logger = logger;

        // Endgültig abgelehnte stille Neuanmeldung (401 ohne gültige
        // Zugangsdaten) → die App wechselt in den Anmeldezustand.
        this.authSession.SessionExpired += OnSessionExpired;

        // Serverseitig verworfenes Geräte-Token (Gate-Ablehnung) →
        // die App wechselt in die Re-Pairing-Eingabe.
        this.deviceTokens.DeviceTokenRejected += OnDeviceTokenRejected;

        // Netzwerkwechsel und Server-Ereignisse laufen auf beliebigen
        // Threads — die Messages gehen über den MainThread in den
        // MVU-Zyklus.
        this.networkMonitor.AvailabilityChanged += OnNetworkAvailabilityChanged;
        this.serverEvents.EventReceived += OnServerEventReceived;
        this.serverEvents.ConnectionClosed += OnServerEventsConnectionClosed;

        // Download-Transfers laufen auf Hintergrundthreads — Zustands-/
        // Fortschrittsmeldungen und Entfernungen gehen über den
        // MainThread in den MVU-Zyklus.
        this.downloads.ItemChanged += OnDownloadItemChanged;
        this.downloads.ItemRemoved += OnDownloadItemRemoved;
    }

    /// <inheritdoc/>
    public async Task HandleAsync(AppEffect effect, CancellationToken cancellationToken = default)
    {
        switch (effect)
        {
            case CheckServerHealthEffect:
                await RunHealthCheckAsync(cancellationToken);
                break;
            case NavigateToPageEffect navigate:
                await MainThread.InvokeOnMainThreadAsync(() => NavigateAsync(navigate.Page));
                break;
            case InitializeStorageEffect:
                await InitializeStorageAsync();
                break;
            case LoadSettingsEffect:
                await LoadSettingsAsync();
                break;
            case PersistSettingsEffect persist:
                SaveSettings(persist.Settings);
                break;
            case LoadServersEffect:
                await LoadServersAsync(cancellationToken);
                break;
            case DiscoverServersEffect:
                await DiscoverServersAsync(cancellationToken);
                break;
            case CheckCandidateServerEffect check:
                await CheckCandidateServerAsync(check.Address, check.DisplayName, cancellationToken);
                break;
            case PersistActiveServerEffect persist:
                await PersistActiveServerAsync(persist.Address, persist.DisplayName);
                break;
            case ExchangePairingCodeEffect exchange:
                await ExchangePairingCodeAsync(exchange.ServerAddress, exchange.Code, cancellationToken);
                break;
            case CheckBootstrapServerEffect check:
                await CheckBootstrapServerAsync(check.Address, check.Ticket, cancellationToken);
                break;
            case ExchangeBootstrapTicketEffect bootstrap:
                await ExchangeBootstrapTicketAsync(bootstrap.ServerAddress, bootstrap.Ticket, cancellationToken);
                break;
            case AttemptAutoLoginEffect autoLogin:
                await AttemptAutoLoginAsync(autoLogin.ServerAddress, cancellationToken);
                break;
            case AttemptLoginEffect login:
                await AttemptLoginAsync(login.ServerAddress, login.Username, login.Password, cancellationToken);
                break;
            case CheckOfflineDataEffect:
                await CheckOfflineDataAsync();
                break;
            case StartNetworkMonitoringEffect:
                await StartNetworkMonitoringAsync();
                break;
            case ConnectServerEventsEffect connect:
                await ConnectServerEventsAsync(connect.ServerAddress, cancellationToken);
                break;
            case DisconnectServerEventsEffect:
                await DisconnectServerEventsAsync();
                break;
            case RunFullSyncEffect:
                await RunFullSyncAsync(cancellationToken);
                break;
            case SyncContinueWatchingEffect:
                await RunPartialSyncAsync("Weiterschauen", s => s.SyncContinueWatchingAsync(cancellationToken));
                break;
            case HideContinueWatchingEntryEffect hide:
                await MutateContinueWatchingAsync(s => s.HideAsync(hide.MediaType, hide.MediaId, cancellationToken));
                break;
            case SkipContinueWatchingEntryEffect skip:
                await MutateContinueWatchingAsync(s => s.SkipAsync(skip.MediaType, skip.MediaId, cancellationToken));
                break;
            case SyncFavoritesEffect:
                await RunPartialSyncAsync("Favoriten", s => s.SyncFavoritesAsync(cancellationToken));
                break;
            case SyncMetadataEffect:
                await RunPartialSyncAsync("Metadaten", s => s.SyncMetadataAsync(cancellationToken));
                break;
            case LoadHomeListsEffect:
                await LoadHomeListsAsync();
                break;
            case LoadHeroBackgroundEffect:
                await LoadHeroBackgroundAsync(cancellationToken);
                break;
            case NavigateToMediaDetailsEffect details:
                await MainThread.InvokeOnMainThreadAsync(() => NavigateToMediaDetailsAsync(details));
                break;
            case LoadSourcesEffect:
                await LoadSourcesAsync(cancellationToken);
                break;
            case NavigateToSourceOverviewEffect overview:
                await MainThread.InvokeOnMainThreadAsync(() => NavigateToSourceOverviewAsync(overview));
                break;
            case LoadSourceGenresEffect genres:
                await LoadSourceGenresAsync(genres, cancellationToken);
                break;
            case LoadSourceItemsEffect items:
                await LoadSourceItemsAsync(items, cancellationToken);
                break;
            case LoadMediaDetailsEffect loadDetails:
                await LoadMediaDetailsAsync(loadDetails, cancellationToken);
                break;
            case LoadMediaDetailsHeaderEffect header:
                await LoadMediaDetailsHeaderAsync(header, cancellationToken);
                break;
            case ToggleFavoriteEffect toggle:
                await ToggleFavoriteAsync(toggle, cancellationToken);
                break;
            case LoadFavoritesPageEffect loadFavorites:
                await LoadFavoritesPageAsync(loadFavorites.RefreshFromServer, cancellationToken);
                break;
            case RemoveFavoriteEffect remove:
                await RemoveFavoriteAsync(remove.FavoriteId, cancellationToken);
                break;
            case LoadPlaylistsEffect:
                await LoadPlaylistsAsync(cancellationToken);
                break;
            case NavigateToPlaylistDetailsEffect details:
                await MainThread.InvokeOnMainThreadAsync(() => NavigateToPlaylistDetailsAsync(details));
                break;
            case LoadPlaylistDetailsEffect details:
                await LoadPlaylistDetailsAsync(details, cancellationToken);
                break;
            case LoadPlaylistEntriesEffect entries:
                await LoadPlaylistEntriesAsync(entries, cancellationToken);
                break;
            case StartPlaylistPlaybackEffect start:
                await StartPlaylistPlaybackAsync(start, cancellationToken);
                break;
            case LoadNextPlaylistEntryEffect next:
                await NavigatePlaylistEntryAsync(
                    next.PlaylistId, next.CurrentEntryId, forward: true, advance: false, cancellationToken);
                break;
            case LoadPreviousPlaylistEntryEffect previous:
                await NavigatePlaylistEntryAsync(
                    previous.PlaylistId, previous.CurrentEntryId, forward: false, advance: false, cancellationToken);
                break;
            case AdvancePlaylistEntryEffect advance:
                await NavigatePlaylistEntryAsync(
                    advance.PlaylistId, advance.CurrentEntryId, forward: true, advance: true, cancellationToken);
                break;
            case SavePlaylistEffect save:
                await SavePlaylistAsync(save, cancellationToken);
                break;
            case DeletePlaylistEffect delete:
                await DeletePlaylistAsync(delete.PlaylistId, cancellationToken);
                break;
            case RemovePlaylistEntryEffect remove:
                await RemovePlaylistEntryAsync(remove, cancellationToken);
                break;
            case MovePlaylistEntryToBeginningEffect move:
                await MovePlaylistEntryToBeginningAsync(move, cancellationToken);
                break;
            case MovePlaylistEntryToEndEffect move:
                await MovePlaylistEntryToEndAsync(move, cancellationToken);
                break;
            case MovePlaylistEntryBetweenEffect move:
                await MovePlaylistEntryBetweenAsync(move, cancellationToken);
                break;
            case ChangePlaylistSortModeEffect sortMode:
                await ChangePlaylistSortModeAsync(sortMode, cancellationToken);
                break;
            case LoadGenreOptionsEffect options:
                await LoadGenreOptionsAsync(options, cancellationToken);
                break;
            case SetPlaylistGenresEffect genres:
                await SetPlaylistGenresAsync(genres, cancellationToken);
                break;
            case ResetPlaylistGenresEffect reset:
                await ResetPlaylistGenresAsync(reset, cancellationToken);
                break;
            case SetPlaylistPublicEffect publish:
                await SetPlaylistPublicAsync(publish, cancellationToken);
                break;
            case LoadPlaylistTargetsEffect:
                await LoadPlaylistTargetsAsync(cancellationToken);
                break;
            case AddPlaylistEntryEffect add:
                await AddPlaylistEntryAsync(add, cancellationToken);
                break;
            case NavigateBackEffect:
                await MainThread.InvokeOnMainThreadAsync(NavigateBackAsync);
                break;
            case LoadProfileInfoEffect:
                await LoadProfileInfoAsync(cancellationToken);
                break;
            case NavigateToPlayerEffect:
                await MainThread.InvokeOnMainThreadAsync(NavigateToPlayerAsync);
                break;
            case ResolvePlaybackSourceEffect resolve:
                await ResolvePlaybackSourceAsync(resolve, cancellationToken);
                break;
            case LoadUpcomingEpisodesEffect loadUpcoming:
                await LoadUpcomingEpisodesAsync(loadUpcoming, cancellationToken);
                break;
            case ReportPlaybackProgressEffect report:
                await ReportPlaybackProgressAsync(report, cancellationToken);
                break;
            case TogglePictureInPictureEffect:
                await TogglePictureInPictureAsync();
                break;
            case LogoutEffect:
                await LogoutAsync();
                break;
            case LoadDownloadsEffect load:
                await LoadDownloadsAsync(load.Settings);
                break;
            case EnqueueDownloadEffect enqueue:
                await EnqueueDownloadAsync(enqueue, cancellationToken);
                break;
            case CancelDownloadEffect cancel:
                await CancelDownloadAsync(cancel, cancellationToken);
                break;
            case DeleteDownloadEffect delete:
                await DeleteDownloadAsync(delete.MediaType, delete.MediaId, cancellationToken);
                break;
            case DeleteAllDownloadsEffect deleteAll:
                await DeleteAllDownloadsAsync(deleteAll.Settings, cancellationToken);
                break;
            case PurgeOrphanedDownloadsEffect purge:
                await PurgeOrphanedDownloadsAsync(purge.Settings, cancellationToken);
                break;
            case DeleteFailedDownloadsEffect deleteFailed:
                await DeleteFailedDownloadsAsync(deleteFailed.Settings, cancellationToken);
                break;
            case ResumeDownloadsEffect resume:
                await ResumeDownloadsAsync(resume.Settings, cancellationToken);
                break;
            case RunAutoDownloadsEffect autoRun:
                await RunAutoDownloadsAsync(autoRun, cancellationToken);
                break;
            case DeleteWatchedDownloadEffect deleteWatched:
                await DeleteDownloadAsync(deleteWatched.MediaType, deleteWatched.MediaId, cancellationToken, completedOnly: true);
                break;
            case SendLogProtocolEffect:
                await SendLogProtocolAsync();
                break;
        }
    }

    /// <summary>
    /// Protokollversand: das Programmprotokoll als Anhang eines
    /// Mailentwurfs an <see cref="LogProtocolRecipient"/> öffnen
    /// (<see cref="Email.ComposeAsync(EmailMessage)"/>). Kann kein
    /// Mailentwurf geöffnet werden (kein Mail-Client, unpackaged
    /// Windows), greift der Teilen-Fallback
    /// (<see cref="Share.RequestAsync(ShareFileRequest)"/>); schlägt auch
    /// dieser fehl, erhält der Anwender einen verständlichen Hinweis über
    /// <see cref="SendLogProtocolCompletedMessage"/>.
    /// </summary>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Melden des Versandergebnisses.</returns>
    private async Task SendLogProtocolAsync()
    {
        var attachments = appLog.GetAttachmentPaths();
        if (!appLog.IsEnabled || attachments.Count == 0)
        {
            await DispatchMessageAsync(
                new SendLogProtocolCompletedMessage(AppUpdate.LogProtocolEmptyError));
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var message = new EmailMessage
                {
                    Subject = "Heimkino – Programmprotokoll",
                    Body = "Programmprotokoll der Heimkino-App (automatisch erzeugt).",
                    BodyFormat = EmailBodyFormat.PlainText,
                    To = new List<string> { LogProtocolRecipient },
                    Attachments = attachments
                        .Select(path => new EmailAttachment(path))
                        .ToList()
                };

                try
                {
                    await Email.ComposeAsync(message);
                }
                catch (Exception mailException) when (mailException is FeatureNotSupportedException
                    || mailException is System.Runtime.InteropServices.COMException
                    || mailException is InvalidOperationException)
                {
                    // Kein Mail-Client verfügbar (unpackaged Windows:
                    // ShowComposeNewEmailAsync ist dort nicht nutzbar) —
                    // Teilen-Dialog als Fallback.
                    logger.LogInformation(
                        "Mailentwurf nicht verfügbar ({Detail}) — Teilen-Fallback.",
                        mailException.Message);
                    await Share.RequestAsync(new ShareFileRequest(
                        "Heimkino – Programmprotokoll",
                        new ShareFile(appLog.LogFilePath)));
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Protokollversand fehlgeschlagen");
            await DispatchMessageAsync(
                new SendLogProtocolCompletedMessage(AppUpdate.LogSendFailedError));
            return;
        }

        await DispatchMessageAsync(new SendLogProtocolCompletedMessage(null));
    }

    private async Task NavigateToPlayerAsync()
    {
        if (Shell.Current is null)
        {
            return;
        }

        try
        {
            await Shell.Current.GoToAsync("player");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Player-Navigation fehlgeschlagen");
        }
    }

    private async Task ResolvePlaybackSourceAsync(ResolvePlaybackSourceEffect resolve, CancellationToken cancellationToken)
    {
        PlaybackSource? source = null;
        string? error = null;
        try
        {
            // Im Offline-Zustand werden nur lokale Dateien aufgelöst —
            // eine unerreichbare Stream-URL würde erst im Player
            // fehlschlagen.
            var allowServerStream = !services.GetRequiredService<MvuHost>().Model.IsOffline;
            source = await services.GetRequiredService<PlaybackSourceService>()
                .ResolveAsync(resolve.MediaType, resolve.MediaId, cancellationToken,
                    allowServerStream: allowServerStream,
                    resumePositionSecondsOverride: resolve.StartPositionSeconds);
            if (source is null)
            {
                error = AppUpdate.PlaybackUnavailableError;
            }
            else
            {
                // Anzeigetitel aus dem Player-State — gesetzt vor dem
                // Effekt (StartPlayback); die Quell-Adresse bleibt
                // absichtlich unprotokolliert (access_token-Query).
                var player = services.GetRequiredService<MvuHost>().Model.Player;
                logger.LogInformation(
                    "Wiedergabe gestartet: {MediaType}/{MediaId} ({Title}) über {Kind}.",
                    resolve.MediaType,
                    resolve.MediaId,
                    player.MediaId == resolve.MediaId ? player.Title : null,
                    source.Kind);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Quellauflösung fehlgeschlagen");
            error = AppUpdate.PlaybackUnavailableError;
        }

        await DispatchMessageAsync(new PlaybackSourceResolvedMessage(
            resolve.MediaType, resolve.MediaId, source, error));
    }

    /// <summary>
    /// Serienkontext einer direkt gestarteten Episode laden (ohne
    /// Detailseite) und die Folge-Episoden in den MVU-Zyklus melden —
    /// ein Fehlschlag ergibt eine leere Fortsetzungsliste statt eines
    /// Fehlerzustands (die Folgevideo-Funktion entfällt dann still).
    /// </summary>
    /// <param name="loadUpcoming">Der Effekt mit Episoden- und Serien-Id.</param>
    /// <param name="cancellationToken">Bricht den Ladevorgang ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Melden der Fortsetzungsliste.</returns>
    private async Task LoadUpcomingEpisodesAsync(LoadUpcomingEpisodesEffect loadUpcoming, CancellationToken cancellationToken)
    {
        IReadOnlyList<PlayerEpisodeRef> episodes = [];
        try
        {
            episodes = await services.GetRequiredService<UpcomingEpisodesService>()
                .LoadAsync(loadUpcoming.MediaId, loadUpcoming.ShowId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Folge-Episoden laden fehlgeschlagen");
        }

        await DispatchMessageAsync(new UpcomingEpisodesLoadedMessage(loadUpcoming.MediaId, episodes));
    }

    private async Task ReportPlaybackProgressAsync(ReportPlaybackProgressEffect report, CancellationToken cancellationToken)
    {
        try
        {
            await services.GetRequiredService<ProgressReportService>()
                .ReportProgressAsync(
                    report.MediaType, (int)report.MediaId,
                    report.Position, report.Duration, report.PlaylistId,
                    cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Routinemäßiger Abbruch (z. B. Wiedergabeende) — kein Fehler.
            throw;
        }
        catch (Exception ex)
        {
            // Ein Fehlschlag ist nicht fatal — der nächste Takt meldet erneut.
            logger.LogWarning(ex, "Fortschrittsmeldung fehlgeschlagen");
        }
    }

    private async Task TogglePictureInPictureAsync()
    {
        var isActive = false;
        try
        {
            isActive = await MainThread.InvokeOnMainThreadAsync(
                () => services.GetRequiredService<IPictureInPictureService>().ToggleAsync());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PiP-Umschaltung fehlgeschlagen");
        }

        // Der Ergebnis-Eintrag obliegt IPictureInPictureService
        // (eine Schicht pro Ereignis) — hier nur der Fehlerpfad.
        await DispatchMessageAsync(new PictureInPictureChangedMessage(isActive));
    }

    private async Task StartNetworkMonitoringAsync()
    {
        try
        {
            networkMonitor.Start();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Netzwerk-Monitoring fehlgeschlagen");
        }

        // Ist-Wert in den MVU-Zyklus melden (ConnectivityChanged meldet
        // nur Wechsel — der Startzustand ginge sonst verloren).
        await DispatchMessageAsync(new NetworkAvailabilityChangedMessage(networkMonitor.IsOnline));
    }

    private async Task ConnectServerEventsAsync(string serverAddress, CancellationToken cancellationToken)
    {
        try
        {
            await serverEvents.ConnectAsync(new Uri(serverAddress), cancellationToken);
            // Der Erfolgs-Eintrag obliegt ServerEventsService (eine
            // Schicht pro Ereignis) — hier nur der Fehlerpfad.
        }
        catch (Exception ex)
        {
            // Ein Fehlschlag ist nicht fatal — der nächste Netzwerkwechsel
            // oder das nächste Ereignis bietet eine neue Gelegenheit.
            logger.LogError(ex, "Ereignisverbindung fehlgeschlagen");
        }
    }

    private async Task DisconnectServerEventsAsync()
    {
        try
        {
            await serverEvents.DisconnectAsync();
            // Der Eintrag obliegt ServerEventsService (eine Schicht
            // pro Ereignis) — hier nur der Fehlerpfad.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trennen der Ereignisverbindung fehlgeschlagen");
        }
    }

    private async Task RunFullSyncAsync(CancellationToken cancellationToken)
    {
        SyncResult result;
        try
        {
            result = await services.GetRequiredService<ServerSyncService>().SynchronizeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Synchronisation fehlgeschlagen");
            result = new SyncResult(0, 0, false, false, false);
        }

        // Der Ergebnis-Eintrag mit den SyncResult-Zählern obliegt
        // ServerSyncService (eine Schicht pro Ereignis) — hier nur
        // der Fehlerpfad.
        await DispatchMessageAsync(new SynchronizationCompletedMessage(result));
    }

    private async Task RunPartialSyncAsync(string listName, Func<ServerSyncService, Task> sync)
    {
        try
        {
            await sync(services.GetRequiredService<ServerSyncService>());
            // Debug statt eines Verlaufseintrags: die Teil-Syncs folgen
            // den Hub-Ereignissen — ContinueWatchingUpdated kann im Takt
            // eingehender Fortschrittsmeldungen feuern und würde den
            // Fortschrittstakt als Einträge ins Protokoll spiegeln
            // (Muster: Hub-Ereignisse in SignalRServerEventsConnection).
            logger.LogDebug("Teil-Synchronisation {List} abgeschlossen.", listName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Teil-Synchronisation fehlgeschlagen");
        }

        // Alle Teil-Synchronisationen betreffen Startseiten-Listen — die
        // Anzeige lädt danach den (ggf. unveränderten) Cache-Stand neu.
        await DispatchMessageAsync(new CachedListsChangedMessage());
    }

    /// <summary>
    /// Weiterschauen-Kontextmenü-Mutation (Ausblenden/Überspringen):
    /// führt den Server-Aufruf samt Cache-Erneuerung bzw. das lokale
    /// Offline-Entfernen aus und meldet bei Erfolg die Cache-Änderung —
    /// die Startseite lädt den neuen Stand ohne manuelle Aktualisierung
    /// nach. Ein Fehlschlag lässt die Liste unverändert (der Eintrag
    /// bleibt sichtbar).
    /// </summary>
    /// <param name="mutate">Die Mutation auf dem aufgelösten <see cref="ContinueWatchingMutationService"/>.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach der Mutation.</returns>
    private async Task MutateContinueWatchingAsync(Func<ContinueWatchingMutationService, Task<bool>> mutate)
    {
        var success = false;
        try
        {
            success = await mutate(services.GetRequiredService<ContinueWatchingMutationService>());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Weiterschauen-Mutation fehlgeschlagen");
        }

        if (success)
        {
            // Die Cache-Kopie wurde aktualisiert (Server-Sync bzw.
            // lokales Entfernen) — die Weiterschauen-Liste lädt den
            // neuen Stand ohne manuelle Aktualisierung nach.
            await DispatchMessageAsync(new CachedListsChangedMessage());
        }
    }

    private async Task LoadHomeListsAsync()
    {
        HomeLists lists;
        try
        {
            // Offline bildet „Weiterschauen" aus den lokalen Downloads
            // (mit Platzhaltern fehlender Folge-Episoden); online kommt
            // die Liste wie bisher aus dem synchronisierten Cache-Stand.
            lists = services.GetRequiredService<MvuHost>().Model.IsOffline
                ? await services.GetRequiredService<OfflineContinueWatchingService>().LoadHomeListsAsync()
                : await services.GetRequiredService<HomeListService>().LoadHomeListsAsync();
        }
        catch (Exception ex)
        {
            // Fehler beim Cache-Lesen → leere Listen, die Startseite bleibt
            // anzeigbar.
            logger.LogError(ex, "Startseiten-Listen laden fehlgeschlagen");
            lists = HomeLists.Empty;
        }

        await DispatchMessageAsync(new HomeListsLoadedMessage(lists));
    }

    private async Task LoadHeroBackgroundAsync(CancellationToken cancellationToken)
    {
        byte[]? imageBytes = null;
        try
        {
            // Der angemeldete Client liefert das servergenerierte Bild
            // (Bearer + stille Token-Erneuerung in der Sende-Pipeline).
            // Ohne Sitzung bzw. bei Fehlschlag bleibt das Bild aus — der
            // Kopfbereich zeigt dann nur Gradienten und Texte.
            var client = authSession.Client;
            if (client is not null)
            {
                var bytes = await client.GetHeroBackgroundAsync(cancellationToken);
                imageBytes = bytes.Length > 0 ? bytes : null;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Hero-Hintergrund laden fehlgeschlagen");
        }

        await DispatchMessageAsync(new HeroBackgroundLoadedMessage(imageBytes));
    }

    private async Task NavigateToMediaDetailsAsync(NavigateToMediaDetailsEffect details)
    {
        if (Shell.Current is null)
        {
            return;
        }

        try
        {
            var route = $"mediadetails?mediaType={Uri.EscapeDataString(details.MediaType)}" +
                $"&id={details.MediaId}" +
                $"&title={Uri.EscapeDataString(details.Title ?? string.Empty)}";
            // Serienkontext-Hints (Episoden-/Staffel-Einträge) nur anhängen,
            // wenn belegt — sie lösen die Serienansicht mit Vorwahl auf.
            if (details.ShowId is > 0)
            {
                route += $"&show={details.ShowId}";
            }
            if (details.SeasonId is > 0)
            {
                route += $"&season={details.SeasonId}";
            }
            if (details.EpisodeId is > 0)
            {
                route += $"&episode={details.EpisodeId}";
            }
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Detail-Navigation fehlgeschlagen");
        }
    }

    private async Task NavigateToSourceOverviewAsync(NavigateToSourceOverviewEffect overview)
    {
        if (Shell.Current is null)
        {
            return;
        }

        try
        {
            if (IsTopmostSourceOverview(overview.SourceId))
            {
                // Die Übersicht dieser Quelle liegt bereits oben auf dem
                // Stack — ein erneuter Push (Tap auf den gerade offenen
                // Menüeintrag) würde eine identische Seite schieben; die
                // Nachlade-Effekte der Auswahl laufen dennoch und
                // frischen den Stand der geöffneten Seite auf.
                return;
            }

            var route = $"sourceoverview?id={overview.SourceId}" +
                $"&name={Uri.EscapeDataString(overview.SourceName ?? string.Empty)}";
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Quellen-Navigation fehlgeschlagen");
        }
    }

    /// <summary>Prüft, ob die oberste Shell-Route bereits die Quellenübersicht der angegebenen Quelle ist.</summary>
    /// <param name="sourceId">Die Quellen-Id des anzusteuernden Ziels.</param>
    /// <returns><c>true</c>, wenn oben bereits „sourceoverview" mit derselben <c>id</c> liegt.</returns>
    private static bool IsTopmostSourceOverview(long sourceId) =>
        IsTopmostRouteWithId("sourceoverview", sourceId);

    /// <summary>
    /// Prüft, ob die oberste Shell-Route eine geschobene Seite der Form
    /// <c>{route}?id={expectedId}</c> ist — Duplikat-Guard der
    /// Detailnavigationen (<c>sourceoverview</c>, <c>playlistdetails</c>).
    /// </summary>
    /// <param name="route">Der Routenname der geschobenen Seite.</param>
    /// <param name="expectedId">Der erwartete Wert des <c>id</c>-Parameters.</param>
    /// <returns><c>true</c>, wenn oben bereits die Route mit derselben <c>id</c> liegt.</returns>
    private static bool IsTopmostRouteWithId(string route, long expectedId)
    {
        var location = Shell.Current?.CurrentState?.Location?.OriginalString;
        if (string.IsNullOrEmpty(location))
        {
            return false;
        }

        // Die Location trägt den ganzen Stack (z. B.
        // „//home/sourceoverview?id=5") — nur das letzte Pfadsegment
        // entscheidet, welche Seite oben liegt.
        var queryIndex = location.IndexOf('?', StringComparison.Ordinal);
        var path = queryIndex >= 0 ? location[..queryIndex] : location;
        var topmost = path.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (!string.Equals(topmost, route, StringComparison.OrdinalIgnoreCase)
            || queryIndex < 0)
        {
            return false;
        }

        foreach (var pair in location[(queryIndex + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2
                && string.Equals(parts[0], "id", StringComparison.OrdinalIgnoreCase))
            {
                return long.TryParse(Uri.UnescapeDataString(parts[1]), out var id)
                    && id == expectedId;
            }
        }

        return false;
    }

    private async Task LoadSourcesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<SourceItem> sources = [];
        string? error = null;
        try
        {
            sources = await services.GetRequiredService<SourceBrowserService>()
                .LoadSourcesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Quellen laden fehlgeschlagen");
            error = "Die Quellen konnten nicht geladen werden.";
        }

        await DispatchMessageAsync(new SourcesLoadedMessage(sources, error));
    }

    private async Task LoadSourceGenresAsync(LoadSourceGenresEffect genres, CancellationToken cancellationToken)
    {
        IReadOnlyList<SourceGenreItem> items;
        try
        {
            items = await services.GetRequiredService<SourceBrowserService>()
                .LoadGenresAsync(genres.SourceId, cancellationToken);
        }
        catch (Exception ex)
        {
            // Genre-Ladefehler sind nicht fatal — die Leiste zeigt dann
            // nur „Alle" (kein Filter).
            logger.LogError(ex, "Genres laden fehlgeschlagen");
            items = [new SourceGenreItem { Id = 0, Name = SourceBrowserService.AllGenresName, IsSelected = true }];
        }

        await DispatchMessageAsync(new SourceGenresLoadedMessage(genres.SourceId, items));
    }

    private async Task LoadSourceItemsAsync(LoadSourceItemsEffect items, CancellationToken cancellationToken)
    {
        SourceItemsPage page;
        string? error = null;
        try
        {
            page = await services.GetRequiredService<SourceBrowserService>()
                .LoadItemsAsync(items.SourceId, items.Page, items.SearchText, items.GenreId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Quelleneinträge laden fehlgeschlagen");
            page = new SourceItemsPage(items.Page, [], HasMore: false);
            error = "Die Einträge konnten nicht geladen werden.";
        }

        await DispatchMessageAsync(new SourceItemsLoadedMessage(
            items.SourceId, page.Page, page.Items, page.HasMore, items.Append, error));
    }

    private async Task LoadMediaDetailsAsync(LoadMediaDetailsEffect loadDetails, CancellationToken cancellationToken)
    {
        MediaDetailsView? details = null;
        string? error = null;
        try
        {
            details = await services.GetRequiredService<MediaDetailsService>()
                .LoadAsync(
                    loadDetails.MediaType, loadDetails.MediaId, loadDetails.Title,
                    loadDetails.ShowId, loadDetails.SeasonId, loadDetails.EpisodeId,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Detail-Ladung fehlgeschlagen");
            error = "Die Details konnten nicht geladen werden.";
        }

        await DispatchMessageAsync(new MediaDetailsLoadedMessage(
            loadDetails.MediaType, loadDetails.MediaId, details, error));
    }

    private async Task LoadMediaDetailsHeaderAsync(LoadMediaDetailsHeaderEffect header, CancellationToken cancellationToken)
    {
        byte[]? imageBytes = null;
        try
        {
            // Episoden-Still-Endpunkt bzw. aufgelöste Bildquelle des
            // Web-Layouts (Bearer + stille Token-Erneuerung in der
            // Sende-Pipeline). Ohne Sitzung bzw. bei Fehlschlag bleibt
            // das Bild aus — der Kopfbereich zeigt dann nur Gradienten
            // und Texte.
            var client = authSession.Client;
            if (client is not null)
            {
                var bytes = header.HeaderEpisodeId is > 0
                    ? await client.GetEpisodeBackgroundImageAsync(header.HeaderEpisodeId.Value, cancellationToken)
                    : header.HeaderPictureId is > 0
                        ? await client.GetPictureAsync(header.HeaderPictureId.Value, cancellationToken)
                        : [];
                imageBytes = bytes.Length > 0 ? bytes : null;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Detail-Kopfbild laden fehlgeschlagen");
        }

        await DispatchMessageAsync(new MediaDetailsHeaderImageLoadedMessage(
            header.MediaType, header.MediaId,
            header.HeaderPictureId, header.HeaderEpisodeId, imageBytes));
    }

    private async Task ToggleFavoriteAsync(ToggleFavoriteEffect toggle, CancellationToken cancellationToken)
    {
        bool? isFavorite = null;
        try
        {
            isFavorite = await services.GetRequiredService<FavoritesService>()
                .ToggleAsync(toggle.MediaType, toggle.MediaId, toggle.Title, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Favoriten-Umschaltung fehlgeschlagen");
        }

        await DispatchMessageAsync(new FavoriteToggleCompletedMessage(
            isFavorite, toggle.MediaType, toggle.MediaId));

        if (isFavorite is not null)
        {
            // Die lokale Favoriten-Kopie wurde aktualisiert — die
            // Startseiten-Liste lädt den neuen Stand ohne manuelle
            // Aktualisierung nach.
            await DispatchMessageAsync(new CachedListsChangedMessage());
        }
    }

    private async Task LoadFavoritesPageAsync(bool refreshFromServer, CancellationToken cancellationToken)
    {
        IReadOnlyList<FavoriteListItem> items = [];
        string? error = null;
        try
        {
            // Online ist der Server führend: erst die lokale Kopie vom
            // Serverstand erneuern, dann den Cache lesen. Schlägt der
            // Sync fehl, bleibt der bisherige Cache-Stand anzeigbar.
            // Offline wird direkt der Cache-Stand geliefert — ohne
            // vergeblichen Server-Aufruf.
            if (refreshFromServer && authSession.Client is not null)
            {
                await services.GetRequiredService<ServerSyncService>()
                    .SyncFavoritesAsync(cancellationToken);
            }

            items = await services.GetRequiredService<HomeListService>()
                .LoadFavoriteItemsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Favoriten laden fehlgeschlagen");
            error = "Die Favoriten konnten nicht geladen werden.";
        }

        await DispatchMessageAsync(new FavoritesPageLoadedMessage(items, error));
    }

    private async Task RemoveFavoriteAsync(long favoriteId, CancellationToken cancellationToken)
    {
        var success = false;
        try
        {
            success = await services.GetRequiredService<FavoritesService>()
                .RemoveFavoriteAsync(favoriteId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Favorit entfernen fehlgeschlagen");
        }

        await DispatchMessageAsync(new FavoriteRemoveCompletedMessage(favoriteId, success));

        if (success)
        {
            // Die lokale Favoriten-Kopie wurde aktualisiert — die
            // Startseiten-Favoritenliste lädt den neuen Stand ohne
            // manuelle Aktualisierung nach.
            await DispatchMessageAsync(new CachedListsChangedMessage());
        }
    }

    private async Task NavigateToPlaylistDetailsAsync(NavigateToPlaylistDetailsEffect details)
    {
        if (Shell.Current is null)
        {
            return;
        }

        try
        {
            if (IsTopmostPlaylistDetails(details.PlaylistId))
            {
                // Die Detailseite dieser Playlist liegt bereits oben auf
                // dem Stack — ein erneuter Push würde eine identische
                // Seite schieben.
                return;
            }

            var route = $"playlistdetails?id={details.PlaylistId}" +
                $"&name={Uri.EscapeDataString(details.Name ?? string.Empty)}";
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlist-Navigation fehlgeschlagen");
        }
    }

    /// <summary>Prüft, ob die oberste Shell-Route bereits die Playlist-Detailseite der angegebenen Playlist ist.</summary>
    /// <param name="playlistId">Die Playlist-Id des anzusteuernden Ziels.</param>
    /// <returns><c>true</c>, wenn oben bereits „playlistdetails" mit derselben <c>id</c> liegt.</returns>
    private static bool IsTopmostPlaylistDetails(long playlistId) =>
        IsTopmostRouteWithId("playlistdetails", playlistId);

    private async Task LoadPlaylistsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<PlaylistListItem> own = [];
        IReadOnlyList<PlaylistListItem> publicPlaylists = [];
        string? error = null;
        try
        {
            var overview = await services.GetRequiredService<PlaylistService>()
                .LoadPlaylistsAsync(cancellationToken);
            own = overview.Own;
            publicPlaylists = overview.Public;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlists laden fehlgeschlagen");
            error = AppUpdate.PlaylistsLoadError;
        }

        await DispatchMessageAsync(new PlaylistsLoadedMessage(own, publicPlaylists, error));
    }

    private async Task LoadPlaylistDetailsAsync(LoadPlaylistDetailsEffect details, CancellationToken cancellationToken)
    {
        PlaylistListItem? info = null;
        var isAdmin = false;
        try
        {
            var service = services.GetRequiredService<PlaylistService>();
            info = await service.LoadPlaylistAsync(details.PlaylistId, cancellationToken);
            // Der IsAdmin-Claim steuert die Sichtbarkeit des
            // Öffentlich-Schalters; er ist nicht fatal — bei Fehlschlag
            // bleibt er ausgeblendet.
            isAdmin = await service.IsCurrentUserAdminAsync();
        }
        catch (Exception ex)
        {
            // Kopf-Nachschlag ist nicht fatal — die Detailseite zeigt
            // dann die Felder der Listenauswahl bzw. den Pfad-Namen.
            logger.LogError(ex, "Playlist-Kopfdaten laden fehlgeschlagen");
        }

        await DispatchMessageAsync(new PlaylistDetailsLoadedMessage(details.PlaylistId, info, isAdmin));
    }

    private async Task LoadPlaylistEntriesAsync(LoadPlaylistEntriesEffect entries, CancellationToken cancellationToken)
    {
        PlaylistEntriesPage page;
        string? error = null;
        try
        {
            page = await services.GetRequiredService<PlaylistService>()
                .LoadEntriesAsync(entries.PlaylistId, entries.Page, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlist-Einträge laden fehlgeschlagen");
            page = new PlaylistEntriesPage(entries.Page, [], HasMore: false, TotalCount: 0);
            error = AppUpdate.PlaylistEntriesLoadError;
        }

        await DispatchMessageAsync(new PlaylistEntriesLoadedMessage(
            entries.PlaylistId, page.Page, page.Items, page.HasMore, page.TotalCount, entries.Append, error));
    }

    // --- Playlist-Verwaltung (Schritt 6) ---

    private async Task SavePlaylistAsync(SavePlaylistEffect save, CancellationToken cancellationToken)
    {
        PlaylistListItem? saved = null;
        string? error = null;
        try
        {
            var service = services.GetRequiredService<PlaylistService>();
            saved = save.EditingPlaylistId is { } playlistId
                ? await service.UpdatePlaylistAsync(
                    playlistId, save.Name, save.Description, cancellationToken)
                : await service.CreatePlaylistAsync(
                    save.Name, save.Description, save.SortMode, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlist speichern fehlgeschlagen");
            error = AppUpdate.PlaylistSaveError;
        }

        await DispatchMessageAsync(new PlaylistFormCompletedMessage(saved, error));
    }

    private async Task DeletePlaylistAsync(long playlistId, CancellationToken cancellationToken)
    {
        string? error = null;
        try
        {
            await services.GetRequiredService<PlaylistService>()
                .DeletePlaylistAsync(playlistId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlist löschen fehlgeschlagen");
            error = AppUpdate.PlaylistDeleteError;
        }

        await DispatchMessageAsync(new PlaylistDeletedMessage(playlistId, error));
    }

    private async Task RemovePlaylistEntryAsync(RemovePlaylistEntryEffect remove, CancellationToken cancellationToken)
    {
        var removed = false;
        var requiresConfirmation = false;
        string? error = null;
        try
        {
            var outcome = await services.GetRequiredService<PlaylistService>()
                .RemoveEntryAsync(
                    remove.PlaylistId,
                    remove.Entry.MediaType,
                    remove.Entry.MediaId,
                    remove.ConfirmContinueWatchingRemoval,
                    cancellationToken);
            removed = outcome.Succeeded;
            requiresConfirmation = outcome.RequiresConfirmation;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlist-Eintrag entfernen fehlgeschlagen");
            error = AppUpdate.PlaylistEntryRemoveError;
        }

        await DispatchMessageAsync(new PlaylistEntryRemoveCompletedMessage(
            remove.Entry, removed, requiresConfirmation, error));
    }

    private async Task MovePlaylistEntryToBeginningAsync(
        MovePlaylistEntryToBeginningEffect move, CancellationToken cancellationToken)
    {
        string? error = null;
        try
        {
            await services.GetRequiredService<PlaylistService>()
                .MoveEntryToBeginningAsync(move.PlaylistId, move.Entry.EntryId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlist-Eintrag an den Anfang verschieben fehlgeschlagen");
            error = AppUpdate.PlaylistEntryMoveError;
        }

        await DispatchMessageAsync(new PlaylistEntryMoveCompletedMessage(move.Entry, error));
    }

    private async Task MovePlaylistEntryToEndAsync(
        MovePlaylistEntryToEndEffect move, CancellationToken cancellationToken)
    {
        string? error = null;
        try
        {
            await services.GetRequiredService<PlaylistService>()
                .MoveEntryToEndAsync(move.PlaylistId, move.Entry.EntryId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlist-Eintrag ans Ende verschieben fehlgeschlagen");
            error = AppUpdate.PlaylistEntryMoveError;
        }

        await DispatchMessageAsync(new PlaylistEntryMoveCompletedMessage(move.Entry, error));
    }

    private async Task MovePlaylistEntryBetweenAsync(
        MovePlaylistEntryBetweenEffect move, CancellationToken cancellationToken)
    {
        string? error = null;
        try
        {
            await services.GetRequiredService<PlaylistService>()
                .MoveEntryBetweenAsync(move.PlaylistId, move.Entry.EntryId, move.NewSortOrder, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlist-Eintrag verschieben fehlgeschlagen");
            error = AppUpdate.PlaylistEntryMoveError;
        }

        await DispatchMessageAsync(new PlaylistEntryMoveCompletedMessage(move.Entry, error));
    }

    private async Task ChangePlaylistSortModeAsync(
        ChangePlaylistSortModeEffect sortMode, CancellationToken cancellationToken)
    {
        PlaylistListItem? updated = null;
        var requiresConfirmation = false;
        string? error = null;
        try
        {
            var result = await services.GetRequiredService<PlaylistService>()
                .ChangeSortModeAsync(
                    sortMode.PlaylistId,
                    sortMode.NewSortMode,
                    sortMode.ConfirmLossOfManualOrder,
                    cancellationToken);
            updated = result.Playlist;
            requiresConfirmation = result.RequiresConfirmation;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlist-Sortiermodus wechseln fehlgeschlagen");
            error = AppUpdate.PlaylistSortModeError;
        }

        await DispatchMessageAsync(new PlaylistSortModeCompletedMessage(
            sortMode.NewSortMode, updated, requiresConfirmation, error));
    }

    private async Task LoadGenreOptionsAsync(LoadGenreOptionsEffect options, CancellationToken cancellationToken)
    {
        IReadOnlyList<PlaylistGenreOptionItem> genreOptions = [];
        string? error = null;
        try
        {
            genreOptions = await services.GetRequiredService<PlaylistService>()
                .LoadGenreOptionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Genre-Optionen laden fehlgeschlagen");
            error = AppUpdate.PlaylistGenreOptionsLoadError;
        }

        await DispatchMessageAsync(new PlaylistGenreOptionsLoadedMessage(genreOptions, error));
    }

    private async Task SetPlaylistGenresAsync(SetPlaylistGenresEffect genres, CancellationToken cancellationToken)
    {
        PlaylistListItem? updated = null;
        string? error = null;
        try
        {
            updated = await services.GetRequiredService<PlaylistService>()
                .SetGenresAsync(genres.PlaylistId, genres.GenreIds, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlist-Genres setzen fehlgeschlagen");
            error = AppUpdate.PlaylistGenresError;
        }

        await DispatchMessageAsync(new PlaylistGenresCompletedMessage(updated, error));
    }

    private async Task ResetPlaylistGenresAsync(ResetPlaylistGenresEffect reset, CancellationToken cancellationToken)
    {
        PlaylistListItem? updated = null;
        string? error = null;
        try
        {
            updated = await services.GetRequiredService<PlaylistService>()
                .ResetGenresAsync(reset.PlaylistId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlist-Genres zurücksetzen fehlgeschlagen");
            error = AppUpdate.PlaylistGenresError;
        }

        await DispatchMessageAsync(new PlaylistGenresCompletedMessage(updated, error));
    }

    private async Task SetPlaylistPublicAsync(SetPlaylistPublicEffect publish, CancellationToken cancellationToken)
    {
        PlaylistListItem? updated = null;
        string? error = null;
        try
        {
            updated = await services.GetRequiredService<PlaylistService>()
                .SetPublicAsync(publish.PlaylistId, publish.IsPublic, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlist-Öffentlich-Flag setzen fehlgeschlagen");
            error = AppUpdate.PlaylistPublicError;
        }

        await DispatchMessageAsync(new PlaylistPublicCompletedMessage(updated, error));
    }

    private async Task LoadPlaylistTargetsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<PlaylistListItem>? targets = null;
        string? error = null;
        try
        {
            targets = await services.GetRequiredService<PlaylistService>()
                .LoadOwnPlaylistsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlist-Auswahlziele laden fehlgeschlagen");
            error = AppUpdate.PlaylistTargetsLoadError;
        }

        await DispatchMessageAsync(new PlaylistTargetsLoadedMessage(targets, error));
    }

    private async Task AddPlaylistEntryAsync(AddPlaylistEntryEffect add, CancellationToken cancellationToken)
    {
        string? resultMessage = null;
        string? error = null;
        try
        {
            resultMessage = await services.GetRequiredService<PlaylistService>()
                .AddMediaToPlaylistAsync(add.PlaylistId, add.MediaType, add.MediaId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Medium zur Playlist hinzufügen fehlgeschlagen");
            error = AppUpdate.PlaylistEntryAddError;
        }

        await DispatchMessageAsync(new PlaylistEntryAddedMessage(
            add.PlaylistName, resultMessage, error));
    }

    /// <summary>
    /// Zur vorherigen Shell-Route zurückkehren („..") — nach dem
    /// Löschen einer Playlist aus ihrer Detailansicht.
    /// </summary>
    private async Task NavigateBackAsync()
    {
        if (Shell.Current is { } shell)
        {
            await shell.GoToAsync("..");
        }
    }

    /// <summary>
    /// Serverseitigen Wiedergabestart einer Playlist ausführen
    /// (<c>POST api/playlists/{id}/play</c>) und den aufgelösten
    /// Starteintrag in den MVU-Zyklus melden — ein Fehlschlag ergibt
    /// eine Fehler-Nachricht, die der Player als Fehlerzustand zeigt.
    /// </summary>
    /// <param name="start">Der Effekt mit Playlist- und optionaler Eintrags-Id.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Melden des Ergebnisses.</returns>
    private async Task StartPlaylistPlaybackAsync(StartPlaylistPlaybackEffect start, CancellationToken cancellationToken)
    {
        PlaylistPlaybackStartInfo? info = null;
        string? error = null;
        try
        {
            info = await services.GetRequiredService<PlaylistService>()
                .StartPlaybackAsync(start.PlaylistId, start.EntryId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlist-Wiedergabestart fehlgeschlagen");
            error = AppUpdate.PlaybackUnavailableError;
        }

        await DispatchMessageAsync(new PlaylistPlaybackStartedMessage(start.PlaylistId, info, error));
    }

    /// <summary>
    /// Eine Playlist-Navigation ausführen — manuell next/previous oder
    /// automatisches advance (<c>POST api/playlists/{id}/play/…</c>) —
    /// und das aufgelöste Ergebnis in den MVU-Zyklus melden. „Kein
    /// Eintrag" (Server-204) geht als <c>null</c>-Ergebnis, ein
    /// Fehlschlag als Fehlertext in die Nachricht.
    /// </summary>
    /// <param name="playlistId">Id der Playlist.</param>
    /// <param name="currentEntryId">Id des Eintrags, von dem aus navigiert wird.</param>
    /// <param name="forward"><c>true</c> = Vorwärtsrichtung (next/advance); <c>false</c> = Rückwärts (previous).</param>
    /// <param name="advance"><c>true</c> = automatisches Weiterschalten (<c>play/advance</c>) statt manuellem <c>play/next</c>.</param>
    /// <param name="cancellationToken">Bricht den Aufruf ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Melden des Ergebnisses.</returns>
    private async Task NavigatePlaylistEntryAsync(
        long playlistId,
        long currentEntryId,
        bool forward,
        bool advance,
        CancellationToken cancellationToken)
    {
        PlaylistNavigationItem? result = null;
        string? error = null;
        try
        {
            var service = services.GetRequiredService<PlaylistService>();
            result = advance
                ? await service.AdvanceAsync(playlistId, currentEntryId, cancellationToken)
                : forward
                    ? await service.GetNextEntryAsync(playlistId, currentEntryId, cancellationToken)
                    : await service.GetPreviousEntryAsync(playlistId, currentEntryId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Playlist-Navigation fehlgeschlagen");
            error = AppUpdate.PlaybackUnavailableError;
        }

        await DispatchMessageAsync(new PlaylistNavigationCompletedMessage(
            playlistId, currentEntryId, result, forward, error));
    }

    private async Task LoadProfileInfoAsync(CancellationToken cancellationToken)
    {
        ProfileInfo info;
        try
        {
            info = await services.GetRequiredService<ProfileInfoService>()
                .LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Ohne lesbare Ablage bleibt die Seite mit Platzhaltern
            // anzeigbar (Benutzername/E-Mail „—").
            logger.LogError(ex, "Profilinformationen laden fehlgeschlagen");
            info = new ProfileInfo(null, null, null);
        }

        await DispatchMessageAsync(new ProfileInfoLoadedMessage(info));
    }

    private async Task LogoutAsync()
    {
        try
        {
            await authSession.LogoutAsync();
            // Der Erfolgs-Eintrag obliegt AuthSessionService (eine
            // Schicht pro Ereignis) — hier nur der Fehlerpfad.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Abmeldung fehlgeschlagen");
        }
    }

    private async Task LoadDownloadsAsync(AppSettings settings)
    {
        IReadOnlyList<DownloadListItem> items = [];
        long usedBytes = 0;
        long orphanedBytes = 0;
        string? error = null;
        try
        {
            // Abgleich Ablageort ↔ Manifest: überholte Teildateien werden
            // entfernt, Restdaten ohne Manifest-Bezug werden erkannt und
            // in der Belegungsanzeige ausgewiesen.
            orphanedBytes = (await downloads.ReconcileAsync()).OrphanedBytes;
            items = await downloads.LoadItemsAsync();
            usedBytes = downloads.GetUsedBytes();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Download-Liste laden fehlgeschlagen");
            error = "Die Downloads konnten nicht geladen werden.";
        }

        await DispatchMessageAsync(new DownloadsLoadedMessage(items, usedBytes, settings.DownloadQuotaGb, error, orphanedBytes));
    }

    private async Task EnqueueDownloadAsync(EnqueueDownloadEffect enqueue, CancellationToken cancellationToken)
    {
        DownloadEnqueueResult result;
        try
        {
            var seriesContext = enqueue.ShowId is null && enqueue.SeasonNumber is null && enqueue.EpisodeNumber is null
                ? null
                : new DownloadSeriesContext(enqueue.ShowId, enqueue.ShowTitle, enqueue.SeasonNumber, enqueue.EpisodeNumber);
            result = await downloads.EnqueueAsync(
                enqueue.MediaType, enqueue.MediaId, enqueue.Title, enqueue.Settings, seriesContext,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Download einreihen fehlgeschlagen");
            result = new DownloadEnqueueResult
            {
                Item = null,
                Notice = "Der Download konnte nicht eingeplant werden.",
                IsError = true
            };
        }

        // Der Einplanungs-Eintrag mit dem Notice-Text obliegt
        // DownloadService (eine Schicht pro Ereignis) — hier nur
        // der Fehlerpfad.
        await DispatchMessageAsync(new DownloadEnqueueCompletedMessage(
            result.Item, result.Notice, result.IsError,
            MediaTypeNames.Normalize(enqueue.MediaType), enqueue.MediaId));
    }

    private async Task DeleteDownloadAsync(
        string mediaType,
        long mediaId,
        CancellationToken cancellationToken,
        bool completedOnly = false)
    {
        try
        {
            // Das Entfernen wird über das ItemRemoved-Ereignis des
            // Dienstes als DownloadDeletedMessage in den MVU-Zyklus
            // gemeldet — keine Rückmeldung nötig.
            if (completedOnly)
            {
                await downloads.DeleteIfCompletedAsync(mediaType, mediaId, cancellationToken);
            }
            else
            {
                await downloads.DeleteAsync(mediaType, mediaId, cancellationToken);
            }

            // Der Lösch-Eintrag obliegt DownloadService (eine Schicht
            // pro Ereignis) — hier nur der Fehlerpfad.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Download löschen fehlgeschlagen");
        }
    }

    private async Task DeleteAllDownloadsAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await downloads.DeleteAllAsync(cancellationToken);
            // Liste + Belegung neu laden (OrphanedBytes aktualisieren).
            await LoadDownloadsAsync(settings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Alle Downloads löschen fehlgeschlagen");
        }
    }

    private async Task PurgeOrphanedDownloadsAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await downloads.PurgeOrphansAsync(cancellationToken);
            await LoadDownloadsAsync(settings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Restdaten bereinigen fehlgeschlagen");
        }
    }

    private async Task DeleteFailedDownloadsAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await downloads.DeleteFailedAsync(cancellationToken);
            // Liste + Belegung neu laden.
            await LoadDownloadsAsync(settings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fehlerhafte Downloads löschen fehlgeschlagen");
        }
    }

    private async Task CancelDownloadAsync(CancelDownloadEffect cancel, CancellationToken cancellationToken)
    {
        try
        {
            await downloads.CancelAsync(cancel.MediaType, cancel.MediaId, cancellationToken);
            // Der Abbruch-Eintrag (Zustand Cancelled) obliegt
            // DownloadService (eine Schicht pro Ereignis) — hier nur
            // der Fehlerpfad.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Download-Abbruch fehlgeschlagen");
        }
    }

    private async Task ResumeDownloadsAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Abgleich Ablageort ↔ Manifest vor der Wiederaufnahme:
            // überholte Teildateien werden entfernt, Restdaten ohne
            // Manifest-Bezug und korrigierte Einträge werden gezählt —
            // ein Fehler des Abgleichs unterbricht die Wiederaufnahme nicht.
            await downloads.ReconcileAsync(cancellationToken);
            await downloads.ResumeInterruptedAsync(settings, cancellationToken);
            // Der Wiederaufnahme-Eintrag mit der Anzahl obliegt
            // DownloadService (eine Schicht pro Ereignis) — hier nur
            // der Fehlerpfad.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Download-Wiederaufnahme fehlgeschlagen");
        }
    }

    private async Task RunAutoDownloadsAsync(RunAutoDownloadsEffect autoRun, CancellationToken cancellationToken)
    {
        try
        {
            await autoDownloads.RunAsync(autoRun.Settings, autoRun.Force, cancellationToken);
            // Der Lauf-Eintrag mit der Einplanungszahl obliegt
            // AutoDownloadService (eine Schicht pro Ereignis) — hier
            // nur der Fehlerpfad.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Auto-Download-Lauf fehlgeschlagen");
        }
    }

    private void OnDownloadItemChanged(object? sender, DownloadItemEventArgs e) =>
        _ = DispatchMessageAsync(new DownloadItemChangedMessage(e.Item));

    private void OnDownloadItemRemoved(object? sender, DownloadRemovedEventArgs e) =>
        _ = DispatchMessageAsync(new DownloadDeletedMessage(e.MediaType, e.MediaId));

    private void OnNetworkAvailabilityChanged(object? sender, bool isOnline)
    {
        // Der Wechsel-Eintrag (inkl. WLAN-Kontext) obliegt dem
        // INetworkStatusMonitor (eine Schicht pro Ereignis) — hier
        // nur der Dispatch.
        _ = DispatchMessageAsync(new NetworkAvailabilityChangedMessage(isOnline));
    }

    private void OnServerEventReceived(object? sender, ServerEventReceivedEventArgs e) =>
        _ = DispatchMessageAsync(new ServerEventReceivedMessage(e.Kind, e.SourceId));

    private void OnServerEventsConnectionClosed(object? sender, EventArgs e) =>
        _ = DispatchMessageAsync(new ServerEventsConnectionClosedMessage());

    private async Task DispatchMessageAsync(AppMessage message)
    {
        try
        {
            var host = services.GetRequiredService<MvuHost>();
            await MainThread.InvokeOnMainThreadAsync(() => host.DispatchAsync(message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dispatch von {MessageType} fehlgeschlagen", message.GetType().Name);
        }
    }

    private async Task AttemptAutoLoginAsync(string serverAddress, CancellationToken cancellationToken)
    {
        AuthAttempt attempt;
        try
        {
            attempt = await authSession.TryStoredLoginAsync(new Uri(serverAddress), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Automatische Anmeldung fehlgeschlagen");
            attempt = new AuthAttempt(AuthAttemptStatus.Failed);
        }

        // Der Anmelde-Eintrag mit Status obliegt AuthSessionService
        // (eine Schicht pro Ereignis) — hier nur der Fehlerpfad.
        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new AutoLoginCompletedMessage(attempt.Status, attempt.Username)));
    }

    private async Task AttemptLoginAsync(string serverAddress, string username, string password, CancellationToken cancellationToken)
    {
        AuthAttempt attempt;
        try
        {
            attempt = await authSession.LoginAsync(new Uri(serverAddress), username, password, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Anmeldung fehlgeschlagen");
            attempt = new AuthAttempt(AuthAttemptStatus.Failed);
        }

        // Der Anmelde-Eintrag mit Status obliegt AuthSessionService
        // (eine Schicht pro Ereignis) — hier nur der Fehlerpfad.
        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new LoginCompletedMessage(attempt.Status)));
    }

    private async Task CheckOfflineDataAsync()
    {
        // Jeder Fehler der Prüfung gilt als „keine Offline-Daten" — die App
        // zeigt dann die Serverauswahl statt im Ladezustand zu verharren.
        var hasOfflineData = false;
        try
        {
            hasOfflineData = await services.GetRequiredService<CacheDatabase>().HasOfflineDataAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Offline-Daten-Prüfung fehlgeschlagen");
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new OfflineDataCheckCompletedMessage(hasOfflineData)));
    }

    private void OnSessionExpired(object? sender, EventArgs e)
    {
        // Der Eintrag obliegt AuthSessionService.OnSessionExpired (eine
        // Schicht pro Ereignis) — hier nur der Dispatch.
        // Der API-Client läuft auf beliebigen Threads — die Message muss
        // über den MainThread in den MVU-Zyklus.
        _ = DispatchSessionExpiredAsync();
    }

    private async Task DispatchSessionExpiredAsync()
    {
        try
        {
            var host = services.GetRequiredService<MvuHost>();
            await MainThread.InvokeOnMainThreadAsync(
                () => host.DispatchAsync(new SessionExpiredMessage()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SessionExpired-Dispatch fehlgeschlagen");
        }
    }

    private async Task InitializeStorageAsync()
    {
        // Fehler der Ablage-Initialisierung dürfen den Start nicht blockieren —
        // die App läuft mit Standardwerten weiter. Beschädigte Datenbanken
        // werden neu angelegt (alte Datei als *.corrupt-* beiseitegelegt)
        // und der Nutzer darüber informiert.
        var recovered = false;
        var failed = false;
        try
        {
            // Die Auflösung des Layouts legt die Verzeichnisse an und wendet den
            // plattformseitigen Backup-Ausschluss an (AppStorageLayoutProvider).
            _ = services.GetRequiredService<AppStorageLayout>();
            recovered |= await services.GetRequiredService<BackupDatabase>().InitializeWithRecoveryAsync();
        }
        catch (Exception ex)
        {
            failed = true;
            logger.LogError(ex, "Backup-Ablage-Initialisierung fehlgeschlagen");
        }

        try
        {
            recovered |= await services.GetRequiredService<CacheDatabase>().InitializeWithRecoveryAsync();
        }
        catch (Exception ex)
        {
            failed = true;
            logger.LogError(ex, "Cache-Ablage-Initialisierung fehlgeschlagen");
        }

        logger.LogInformation(
            "Ablage initialisiert (Wiederherstellung: {Recovered}).",
            recovered);
        if (recovered)
        {
            await DispatchMessageAsync(new StorageRecoveredMessage(
                "Die lokalen Daten waren beschädigt und wurden neu initialisiert. " +
                "Zwischengespeicherte Inhalte werden neu synchronisiert."));
        }
        else if (failed)
        {
            await DispatchMessageAsync(new StorageRecoveredMessage(
                "Ein Teil der lokalen Daten konnte nicht gelesen werden. " +
                "Die App läuft mit eingeschränkten lokalen Daten weiter."));
        }
    }

    private async Task LoadSettingsAsync()
    {
        AppSettings settings;
        try
        {
            settings = services.GetRequiredService<AppSettingsService>().Load();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Einstellungen laden fehlgeschlagen");
            settings = new AppSettings();
        }

        appLog.SetEnabled(settings.LoggingEnabled);

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new SettingsLoadedMessage(settings)));
    }

    private void SaveSettings(AppSettings settings)
    {
        try
        {
            services.GetRequiredService<AppSettingsService>().Save(settings);
            // Laufzeit-Umschaltung der Protokollierung unmittelbar nach dem
            // erfolgreichen Schreiben — ohne Neustart wirksam.
            appLog.SetEnabled(settings.LoggingEnabled);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Einstellungen speichern fehlgeschlagen");
        }
    }

    private async Task LoadServersAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<KnownServer> servers = [];
        string? activeAddress = null;
        try
        {
            var knownServers = services.GetRequiredService<KnownServersService>();
            await knownServers.MigrateLegacyServerAddressAsync();
            servers = await knownServers.GetAllAsync();

            var configured = await services
                .GetRequiredService<IServerAddressProvider>()
                .GetConfiguredServerAddressAsync(cancellationToken);
            activeAddress = configured?.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Serververwaltung laden fehlgeschlagen");
        }

        // Geräte-Token des aktiven Servers laden und den Gate-Provider
        // auf dessen Server-Kontext einstellen; ohne Token fällt die
        // Auflösung auf die statischen Quellen zurück.
        await deviceTokens.LoadForServerAsync(activeAddress, cancellationToken);
        var pairingRequired = await IsPairingRequiredAsync(activeAddress);

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new ServersLoadedMessage(servers, activeAddress, pairingRequired)));
    }

    private async Task DiscoverServersAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<DiscoveredServer> found = [];
        try
        {
            found = await services
                .GetRequiredService<ServerDiscoveryService>()
                .DiscoverAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Server-Erkennung fehlgeschlagen");
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new ServerDiscoveryCompletedMessage(found)));
    }

    private async Task CheckCandidateServerAsync(string address, string? displayName, CancellationToken cancellationToken)
    {
        var success = false;
        var pairingRequired = false;
        try
        {
            var result = await healthProbe.CheckAsync(new Uri(address), cancellationToken);
            if (result == HealthCheckResult.GateRejected)
            {
                // Das Gate hat geantwortet — der Server ist erreichbar.
                // Die Probe löst den Gate-Schlüssel adressspezifisch auf:
                // ein 401 beweist die Ungültigkeit des *hinterlegten*
                // Geräte-Tokens dieses Kandidaten — es wird gezielt
                // gelöscht, auch wenn der Server nicht aktiv ist (Muster:
                // AuthSessionService.HandleGateRejectionAsync). Sonst
                // würde IsPairingRequiredAsync fälschlich „kein Pairing
                // nötig" melden und ein aussichtsloser Auto-Login starten.
                try
                {
                    await secrets.ClearDeviceTokenAsync(address);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Geräte-Token des Kandidaten konnte nicht verworfen werden");
                }

                // Provider-Rücksetzung nur, wenn der Kandidat der
                // aktuell bediente Server ist.
                await deviceTokens.DiscardIfCurrentAsync(address);
            }

            success = result != HealthCheckResult.Unreachable;
            if (success)
            {
                pairingRequired = await IsPairingRequiredAsync(address);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kandidatenprüfung fehlgeschlagen");
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new ServerCheckCompletedMessage(address, displayName, success, pairingRequired)));
    }

    private async Task PersistActiveServerAsync(string address, string? displayName)
    {
        try
        {
            await services.GetRequiredService<KnownServersService>()
                .SetActiveServerAsync(address, displayName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Server speichern fehlgeschlagen");
        }

        // Provider auf das Geräte-Token des neuen Servers umstellen, bevor
        // der nachfolgende Auto-Login-Effekt läuft (Effekte laufen
        // sequenziell).
        await deviceTokens.LoadForServerAsync(address);
    }

    private async Task RunHealthCheckAsync(CancellationToken cancellationToken)
    {
        // Jeder Fehler der Prüfung führt zu „nicht erreichbar" — die App darf
        // nicht auf der Ladeanzeige hängen bleiben.
        var result = HealthCheckResult.Unreachable;
        try
        {
            result = await healthProbe.CheckAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreichbarkeitsprüfung fehlgeschlagen");
        }

        if (result == HealthCheckResult.GateRejected)
        {
            // Das bereitgestellte Geräte-Token wurde verworfen — zuerst
            // entfernen und auf die statische Auflösung zurückfallen,
            // dann einmalig erneut prüfen: ein funktionierender
            // statischer Schlüssel wird sonst vom veralteten Token
            // verdeckt (nahtlose Migration).
            await deviceTokens.DiscardCurrentAsync();
            try
            {
                result = await healthProbe.CheckAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreichbarkeits-Nachprüfung fehlgeschlagen");
                result = HealthCheckResult.Unreachable;
            }

            if (result == HealthCheckResult.GateRejected)
            {
                // Es gibt keinen nutzbaren Schlüssel — Re-Pairing
                // einfordern (Ereignis → DeviceTokenRevokedMessage).
                deviceTokens.NotifyRejected();
                return;
            }
        }

        // Der Ergebnis-Eintrag obliegt ServerHealthProbe (eine Schicht
        // pro Ereignis) — hier nur der Fehlerpfad.
        var pairingRequired = result == HealthCheckResult.Reachable
            && await IsPairingRequiredAsync(services.GetRequiredService<MvuHost>().Model.ActiveServerAddress);

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new ServerHealthCheckCompletedMessage(
                result == HealthCheckResult.Reachable, pairingRequired)));
    }

    /// <summary>
    /// Ermittelt, ob für den Server ein Geräte-Pairing erforderlich ist:
    /// kein hinterlegtes Geräte-Token UND keine statische Gate-Quelle
    /// konfiguriert (<see cref="ClientGate.ResolveStatic"/> liefert den
    /// Platzhalter).
    /// </summary>
    /// <param name="serverAddress">Die zu prüfende Serveradresse.</param>
    /// <returns><c>true</c>, wenn ein Pairing eingefordert werden muss.</returns>
    private async Task<bool> IsPairingRequiredAsync(string? serverAddress)
    {
        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            return false;
        }

        try
        {
            if (await secrets.HasDeviceTokenAsync(serverAddress))
            {
                return false;
            }

            return ClientGate.ResolveStatic() == ClientGate.ApiTokenPlaceholder;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pairing-Erforderlichkeit fehlgeschlagen");
            return false;
        }
    }

    private async Task ExchangePairingCodeAsync(string serverAddress, string code, CancellationToken cancellationToken)
    {
        PairingStatus status;
        try
        {
            var token = await pairing.ExchangeAsync(
                new Uri(serverAddress), code, GetDeviceName(), cancellationToken);

            // Provider-/Client-Aktivierung nur, wenn der Pairing-Kontext
            // noch aktuell ist — dieselbe Bedingung wie der Stale-Guard in
            // UpdatePairingCompleted (IsPairingRunning + aktiver Server):
            // ein zwischenzeitlich gestarteter Servercheck (ConnectButton/
            // Serverliste bleiben aktiv) hat den Provider über
            // PersistActiveServerAsync → LoadForServerAsync bereits auf den
            // neuen Server umgestellt — ein verspätetes Ergebnis des alten
            // Servers darf ihn nicht zurücküberschreiben (der Stale-Guard
            // verwirft nur die Nachricht, nicht die Seiteneffekte).
            var model = services.GetRequiredService<MvuHost>().Model;
            var pairingCurrent = model.IsPairingRunning
                && ServerAddress.TryNormalize(model.ActiveServerAddress, out var activeAddress)
                && ServerAddress.TryNormalize(serverAddress, out var pairedAddress)
                && string.Equals(activeAddress, pairedAddress, StringComparison.OrdinalIgnoreCase);
            if (pairingCurrent)
            {
                // Atomarer Stale-Guard: persistiert + aktiviert das Token
                // nur, wenn die Adresse noch der aktuell bediente Server
                // ist (unter Lock geprüft — ein paralleler Serverwechsel
                // über LoadForServerAsync kann nicht dazwischenfunken);
                // andernfalls bleibt das Token nur unter seiner Adresse
                // hinterlegt. Bei Aktivierung erhält der gehaltene
                // API-Client den Schlüssel direkt.
                var activated = await deviceTokens.SetPairedTokenIfCurrentAsync(
                    serverAddress, token, cancellationToken);
                if (activated)
                {
                    authSession.Client?.SetApiKey(token);
                }
            }
            else
            {
                // Veralteter Kontext: das gültige Token bleibt unter seiner
                // eigenen Serveradresse hinterlegt (ein späteres Pairing zu
                // diesem Server entfällt), ohne den aktiven Provider oder
                // den gehaltenen API-Client anzufassen.
                await secrets.SetDeviceTokenAsync(serverAddress, token);
            }

            status = PairingStatus.Succeeded;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ApiException ex)
        {
            status = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => PairingStatus.InvalidCode,
                HttpStatusCode.TooManyRequests => PairingStatus.RateLimited,
                HttpStatusCode.BadRequest => PairingStatus.InvalidRequest,
                HttpStatusCode.NotFound => PairingStatus.NotSupported,
                null => PairingStatus.ServerUnreachable,
                _ => PairingStatus.Failed
            };
            logger.LogError(ex, "Pairing-Austausch fehlgeschlagen");
        }
        catch (Exception ex)
        {
            // Ungültige/nicht entschlüsselbare Antwort oder sonstiger
            // Fehler → allgemeiner Pairing-Fehler.
            logger.LogError(ex, "Pairing-Austausch fehlgeschlagen");
            status = PairingStatus.Failed;
        }

        await DispatchMessageAsync(new PairingCompletedMessage(status));
    }

    /// <summary>
    /// Prüft die Erreichbarkeit des Servers aus einer Ticket-Adresse
    /// (QR-/Ticket-Onboarding): erst bei Erfolg übernimmt das Update den
    /// Server und startet den Bootstrap-Austausch. Eine Gate-Ablehnung
    /// zählt als „erreichbar" — der Server hat geantwortet; das Bootstrap
    /// liefert ohnehin ein frisches Geräte-Token.
    /// </summary>
    /// <param name="address">Adresse aus der Ticket-Adresse.</param>
    /// <param name="Ticket">Das einzulösende Ticket (wird an die Ergebnis-Nachricht weitergereicht).</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    private async Task CheckBootstrapServerAsync(string address, string Ticket, CancellationToken cancellationToken)
    {
        var success = false;
        try
        {
            var result = await healthProbe.CheckAsync(new Uri(address), cancellationToken);
            success = result != HealthCheckResult.Unreachable;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bootstrap-Serverprüfung fehlgeschlagen");
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new BootstrapServerCheckedMessage(address, success, Ticket)));
    }

    /// <summary>
    /// Löst das Ticket gegen das verschlüsselte Session-Paket ein
    /// (<c>api/pairing/bootstrap</c>) und persistiert Geräte-Token,
    /// Anmelde-Token und Refresh-Token — nur wenn der Bootstrap-Kontext
    /// noch aktuell ist (Staler-Guard wie <see cref="ExchangePairingCodeAsync"/>).
    /// </summary>
    /// <param name="serverAddress">Adresse des Bootstrap-Servers.</param>
    /// <param name="ticket">Das einzulösende Ticket bzw. der Kurzcode.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    private async Task ExchangeBootstrapTicketAsync(string serverAddress, string ticket, CancellationToken cancellationToken)
    {
        BootstrapStatus status;
        try
        {
            var payload = await pairing.BootstrapAsync(
                new Uri(serverAddress), ticket, GetDeviceName(), cancellationToken);

            // Provider-/Session-Aktivierung nur, wenn der Bootstrap-Kontext
            // noch aktuell ist — ein zwischenzeitlich gestarteter
            // Servercheck hat den Provider bereits umgestellt; die Session
            // (JWT + Refresh-Token) gehört zudem zum Server des Tickets und
            // darf bei veraltetem Kontext nicht global hinterlegt werden.
            var model = services.GetRequiredService<MvuHost>().Model;
            var bootstrapCurrent = model.IsBootstrapRunning
                && ServerAddress.TryNormalize(model.ActiveServerAddress, out var activeAddress)
                && ServerAddress.TryNormalize(serverAddress, out var pairedAddress)
                && string.Equals(activeAddress, pairedAddress, StringComparison.OrdinalIgnoreCase);
            if (bootstrapCurrent)
            {
                // Atomarer Stale-Guard: persistiert + aktiviert das
                // Geräte-Token nur, wenn die Adresse noch der aktuell
                // bediente Server ist (Muster: ExchangePairingCodeAsync).
                var activated = await deviceTokens.SetPairedTokenIfCurrentAsync(
                    serverAddress, payload.DeviceToken, cancellationToken);
                if (activated)
                {
                    authSession.Client?.SetApiKey(payload.DeviceToken);
                }

                // Sitzung des Bootstrap-Pakets persistieren — die
                // anschließende automatische Anmeldung validiert das
                // Token und führt direkt zur Startseite.
                await secrets.SetAuthTokenAsync(payload.Token);
                await secrets.SetRefreshTokenAsync(payload.RefreshToken);
            }
            else
            {
                // Veralteter Kontext: das Geräte-Token bleibt unter seiner
                // eigenen Adresse hinterlegt; die Session wird verworfen,
                // weil sie dem Ticket-Server gehört, nicht dem aktiven.
                await secrets.SetDeviceTokenAsync(serverAddress, payload.DeviceToken);
            }

            status = BootstrapStatus.Succeeded;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ApiException ex)
        {
            status = ex.StatusCode switch
            {
                HttpStatusCode.Unauthorized => BootstrapStatus.InvalidTicket,
                HttpStatusCode.TooManyRequests => BootstrapStatus.RateLimited,
                HttpStatusCode.BadRequest => BootstrapStatus.InvalidRequest,
                HttpStatusCode.NotFound => BootstrapStatus.NotSupported,
                null => BootstrapStatus.ServerUnreachable,
                _ => BootstrapStatus.Failed
            };
            logger.LogError(ex, "Bootstrap-Austausch fehlgeschlagen");
        }
        catch (Exception ex)
        {
            // Ungültige/nicht entschlüsselbare Antwort oder sonstiger
            // Fehler → allgemeiner Bootstrap-Fehler.
            logger.LogError(ex, "Bootstrap-Austausch fehlgeschlagen");
            status = BootstrapStatus.Failed;
        }

        await DispatchMessageAsync(new BootstrapCompletedMessage(status));
    }

    /// <summary>
    /// Anzeigename des Geräts für das Pairing (<see cref="DeviceInfo.Name"/>,
    /// Fallback Hersteller + Modell; <c>null</c>, wenn nichts ermittelbar —
    /// der Server nutzt dann seinen Fallback). Auf 200 Zeichen gekürzt.
    /// </summary>
    /// <returns>Der Gerätename oder <c>null</c>.</returns>
    private static string? GetDeviceName()
    {
        var name = DeviceInfo.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"{DeviceInfo.Manufacturer} {DeviceInfo.Model}".Trim();
        }

        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        return trimmed.Length > 200 ? trimmed[..200] : trimmed;
    }

    private void OnDeviceTokenRejected(object? sender, EventArgs e)
    {
        // Der Ablehnungs-Eintrag obliegt DeviceTokenService.NotifyRejected
        // (eine Schicht pro Ereignis) — hier nur der Dispatch.
        _ = DispatchMessageAsync(new DeviceTokenRevokedMessage());
    }

    private async Task NavigateAsync(AppPage page)
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window is null)
        {
            return;
        }

        // Die Server-Einrichtung/-verwaltung hat keinen Menüeintrag: beim
        // Erststart wird sie als Window-Root gesetzt (die Shell existiert noch
        // nicht), aus den Einstellungen heraus als geschobene Shell-Route.
        if (page == AppPage.ServerSetup)
        {
            if (window.Page is AppShell && Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("serversetup");
            }
            else if (window.Page is not Pages.ServerSetupPage)
            {
                window.Page = services.GetRequiredService<Pages.ServerSetupPage>();
            }

            return;
        }

        // Die Anmeldemaske hat keinen Menüeintrag: beim Start wird sie als
        // Window-Root gesetzt (die Shell existiert noch nicht), bei
        // Sitzungsablauf als geschobene Shell-Route.
        if (page == AppPage.Login)
        {
            if (window.Page is AppShell && Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("login");
            }
            else if (window.Page is not Pages.LoginPage)
            {
                window.Page = services.GetRequiredService<Pages.LoginPage>();
            }

            return;
        }

        // Übergang von der Start-Ladeanzeige zur Hauptnavigation.
        if (window.Page is not AppShell)
        {
            window.Page = services.GetRequiredService<AppShell>();

            // Shell.Current wird erst beim Anhängen der Shell gesetzt.
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (Shell.Current is null && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
            }
        }

        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync($"//{AppRoutes.ToRoute(page)}");
        }
    }
}
